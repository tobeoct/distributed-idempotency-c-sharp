using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using DistributedIdempotency.Attributes;
using System.Reflection;
using DistributedIdempotency.Helpers;
using DistributedIdempotency.Logic;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace DistributedIdempotency.Behaviours
{
    public class IdempotencyInterceptor : IActionFilter
    {
        private readonly IdempotencyService _idempotencyService;
        Stopwatch _lifetimeStopwatch;
        public IdempotencyInterceptor(IdempotencyService idempotencyService)
        {
            _idempotencyService = idempotencyService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _lifetimeStopwatch = Stopwatch.StartNew();

            var idempotentAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<IdempotentAttribute>()
                .FirstOrDefault();

            if (idempotentAttribute != null)
            {
                var idempotencyKey = DetermineIdempotencyKey(context);
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    context.HttpContext.Items[Constants.IDEMPOTENCY_KEY] = idempotencyKey;
                    var isDuplicate =  _idempotencyService.CheckForDuplicateAsync(idempotencyKey).Result;
                    if (isDuplicate)
                    {
                        ResolveDuplicate(context, idempotencyKey, idempotentAttribute.TimeOut);
                        return;
                    }
                    Run(() => _idempotencyService.UpsertAsync(idempotencyKey, window: idempotentAttribute.Window), idempotentAttribute.StrictMode).Wait();

                }
            }

            _lifetimeStopwatch.Stop();
            Console.WriteLine($"Idempotent API Performance: before action executes => {_lifetimeStopwatch.ElapsedMilliseconds}ms");
        }

        string DetermineIdempotencyKey(ActionExecutingContext context)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var key = DetermineKeyFromAttribute(context);
            stopwatch.Stop();
            Console.WriteLine($"DetermineIdempotencyKey Performance: {stopwatch.ElapsedMilliseconds}ms");
            return key;
        }
        string DetermineKeyFromAttribute(ActionExecutingContext context)
        {

            List<(string propertyName, int? order, object value)> keyComponents = [];

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();


            var controllerType = context.Controller.GetType();
            var methodInfo = controllerType.GetMethod(actionName);

            if (methodInfo != null)
            {
                var parameters = methodInfo.GetParameters();

                foreach (var parameter in parameters)
                {
                    var components = ExtractKeyComponentsFromParameter(context, parameter);
                    keyComponents.AddRange(components);
                }
            }

            var orderedKeyComponents = keyComponents.OrderBy(obj => obj.order)
            .ThenBy(obj => obj.propertyName).Select(k => k.value);

            return string.Join('.', orderedKeyComponents);
        }
        static List<(string name, int? order, object value)> ExtractKeyComponentsFromParameter(ActionExecutingContext context, ParameterInfo parameter)
        {
            List<(string propertyName, int? order, object value)> keyComponents = [];
            object? parameterValue;
            IdempotencyKeyAttribute? attribute;
            if (parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string))
            {
                PropertyInfo[] properties = parameter.ParameterType.GetProperties();
                _ = context.ActionArguments.TryGetValue(parameter.Name, out parameterValue);
                if (parameterValue == null) return keyComponents;

                foreach (PropertyInfo property in properties)
                {
                    if (!Attribute.IsDefined(property, typeof(IdempotencyKeyAttribute), false)) continue;

                    attribute = property.GetCustomAttributes(true)?
                       .OfType<IdempotencyKeyAttribute>()?
                       .FirstOrDefault();
                    object propertyValue = property.GetValue(parameterValue);
                    keyComponents.Add((property.Name, attribute?.Order, propertyValue));
                }
                return keyComponents;

            }

            attribute = parameter.GetCustomAttribute<IdempotencyKeyAttribute>();
            if (attribute == null) return keyComponents;

            parameterValue = context.ActionArguments[parameter.Name];
            keyComponents.Add((parameter.Name, attribute?.Order, parameterValue));
            return keyComponents;
        }
        void ResolveDuplicate(ActionExecutingContext context, string idempotencyKey, int timeout)
        {
            var response = _idempotencyService.GetResponseAsync(idempotencyKey, timeout)?.Result;
            if (response?.Response == null) context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
            else
            {
                var result = new ObjectResult(response.Response)
                {
                    StatusCode = response.StatusCode
                };
                context.Result = result;
            }
        }


        public void OnActionExecuted(ActionExecutedContext context)
        {
            _lifetimeStopwatch.Start();
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
            _lifetimeStopwatch.Stop();
            Console.WriteLine($"Idempotent API Performance: after action executes => Total Time: {_lifetimeStopwatch.ElapsedMilliseconds}ms");

        }
        private static async Task Run(Func<Task> action, bool isStrict)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (isStrict) await action();
            else action.Invoke();
            stopwatch.Stop();
            Console.WriteLine($"StrictMode:{Configuration.StrictMode} Run Performance: {stopwatch.ElapsedMilliseconds}ms");

        }
    }


}
