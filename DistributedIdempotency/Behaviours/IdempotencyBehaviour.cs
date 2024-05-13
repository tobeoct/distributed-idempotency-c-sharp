using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using DistributedIdempotency.Attributes;
using System.Reflection;
using DistributedIdempotency.Helpers;
using DistributedIdempotency.Logic;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Runtime.InteropServices;
using System;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace DistributedIdempotency.Behaviours
{
    public class IdempotencyInterceptor : IActionFilter
    {
        private readonly IdempotencyService _idempotencyService;
        private readonly string _defaultNamespace = typeof(IdempotencyInterceptor).Namespace;
        static ConcurrentDictionary<string, Func<object[], string>> KeyExtractors = new ConcurrentDictionary<string, Func<object[], string>>();
        DateTime NextExtractorRefresh;
        public IdempotencyInterceptor(IdempotencyService idempotencyService)
        {
            _idempotencyService = idempotencyService;
            NextExtractorRefresh = DateTime.Now.AddHours(1);
        }

        Stopwatch stopwatch;
        public void OnActionExecuting(ActionExecutingContext context)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            var idempotentAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<IdempotentAttribute>()
                .FirstOrDefault();

            if (idempotentAttribute != null)
            {
                var idempotencyKey = DetermineIdempotencyKey(context);
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    context.HttpContext.Items[Constants.IDEMPOTENCY_KEY] = idempotencyKey;

                    var isDuplicate = _idempotencyService.CheckForDuplicateAsync(idempotencyKey).Result;
                    if (isDuplicate)
                    {
                        var response = _idempotencyService.GetResponseAsync(idempotencyKey, idempotentAttribute.TimeOut)?.Result;
                        if (response?.Response == null) context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
                        else
                        {
                            var result = new ObjectResult(response.Response)
                            {
                                StatusCode = response.StatusCode
                            };
                            context.Result = result;
                        }
                        return;
                    }
                    Run(() => _idempotencyService.UpsertAsync(idempotencyKey, window: idempotentAttribute.Window), idempotentAttribute.StrictMode).Wait();

                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Idempotent API Performance: before action executes => {stopwatch.ElapsedMilliseconds}ms");
        }


        string DetermineIdempotencyKey(ActionExecutingContext context)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var key = DetermineIdempotencyKeyFromIdempotencyKeyAttribute(context);
            //if (string.IsNullOrEmpty(key)) key = DetermineIdempotencyKeyFromIdempotentAttribute(idempotentAttribute, context);
            stopwatch.Stop();
            Console.WriteLine($"DetermineIdempotencyKey Performance: {stopwatch.ElapsedMilliseconds}ms");
            return key;
        }
        string DetermineIdempotencyKeyFromIdempotencyKeyAttribute(ActionExecutingContext context)
        {

            List<(string propertyName, int? order, object value)> keyComponents = [];

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();


            var controllerType = context.Controller.GetType();
            var methodInfo = controllerType.GetMethod(actionName);

            if (methodInfo != null)
            {
                // Get the parameters of the action method
                var parameters = methodInfo.GetParameters();

                foreach (var parameter in parameters)
                {
                    if (parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string))
                    {
                        PropertyInfo[] properties = parameter.ParameterType.GetProperties();

                        foreach (PropertyInfo property in properties)
                        {
                            if (Attribute.IsDefined(property, typeof(IdempotencyKeyAttribute)))
                            {
                                var attribute = property.GetCustomAttributes(true)?
                                    .OfType<IdempotencyKeyAttribute>()?
                                    .FirstOrDefault();
                                object propertyValue = property.GetValue(parameter);
                                // Do something with the property value
                                Console.WriteLine($"Property '{property.Name}' value: {propertyValue}");
                                if (!keyComponents.Any(k => k.propertyName == property.Name)) keyComponents.Add((property.Name, attribute?.Order, propertyValue));
                            }
                        }
                    }
                    else
                    {
                        // Check if the parameter has the desired attribute
                        var attribute = parameter.GetCustomAttribute<IdempotencyKeyAttribute>();
                        if (attribute != null)
                        {
                            // Get the value of the parameter
                            var parameterValue = context.ActionArguments[parameter.Name];
                            Console.WriteLine($"Action argument '{parameter.Name}' value: {parameterValue}");
                            if (!keyComponents.Any(k => k.propertyName == parameter.Name)) keyComponents.Add((parameter.Name, attribute?.Order, parameterValue));
                        }
                    }
                }
            }

            var orderedKeyComponents = keyComponents.OrderBy(obj => obj.order)
            .ThenBy(obj => obj.propertyName).Select(k => k.value);

            return string.Join('.', orderedKeyComponents);
        }
        string DetermineIdempotencyKeyFromIdempotentAttribute(IdempotentAttribute idempotentAttribute, ActionExecutingContext context)
        {
            if (DateTime.Now > NextExtractorRefresh) KeyExtractors = new ConcurrentDictionary<string, Func<object[], string>>();
            if (idempotentAttribute == null) return string.Empty;

            var keyExtractorNamespace = idempotentAttribute.KeyExtractorNamespace ?? _defaultNamespace;
            var keyExtractorMethodName = idempotentAttribute.KeyExtractorMethodName;
            var keyExtractorClassName = idempotentAttribute.KeyExtractorClassName;
            var method = $"{keyExtractorNamespace}.{keyExtractorClassName}.{keyExtractorMethodName}";
            KeyExtractors.TryGetValue(method, out var keyExtractor);
            if (keyExtractor == null)
            {
                keyExtractor = string.IsNullOrEmpty(keyExtractorMethodName)
                ? DefaultKeyExtractor
                : GetCustomKeyExtractor(keyExtractorNamespace, keyExtractorClassName, keyExtractorMethodName);
                KeyExtractors.TryAdd(method, keyExtractor);
            }
            var arguments = context.ActionArguments.Values.ToArray();
            return keyExtractor(arguments);

        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            stopwatch.Restart();
            var idempotentAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<IdempotentAttribute>()
                .FirstOrDefault();

            if (idempotentAttribute != null)
            {

                var idempotencyKey = context.HttpContext.Items[Constants.IDEMPOTENCY_KEY]?.ToString();
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    Run(() => _idempotencyService.UpsertAsync(idempotencyKey, context.Result, false, window: idempotentAttribute.Window), idempotentAttribute.StrictMode).Wait();

                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Idempotent API Performance: after action executes => {stopwatch.ElapsedMilliseconds}ms");

        }
        private static Func<object[], string> GetCustomKeyExtractor(string targetNamespace, string targetClassName, string targetMethodName)
        {
            Type targetType = Type.GetType($"{targetNamespace}.{targetClassName}");

            if (targetType != null)
            {
                MethodInfo targetMethod = targetType.GetMethod(targetMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (targetMethod != null)
                {
                    var keyExtractorDelegate = (Func<object[], string>)Delegate.CreateDelegate(typeof(Func<object[], string>), null, targetMethod);
                    return keyExtractorDelegate;
                }

                throw new ArgumentException($"Method '{targetMethodName}' not found in class '{targetClassName}'.");
            }

            throw new ArgumentException($"Class '{targetClassName}' not found in namespace '{targetNamespace}'.");

        }

        private string DefaultKeyExtractor(object[] args)
        {

            return ChecksumHelper.GetMD5Checksum(args);
        }

        private static async Task Run(Func<Task> action, bool isStrict)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (isStrict) await action();
            else action.Invoke();
            stopwatch.Stop();
            Console.WriteLine($"Run Performance: {stopwatch.ElapsedMilliseconds}ms");

        }
    }


}
