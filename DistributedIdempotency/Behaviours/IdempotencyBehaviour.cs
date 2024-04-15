using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using DistributedIdempotency.Attributes;
using System.Reflection;
using DistributedIdempotency.Helpers;
using DistributedIdempotency.Logic;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

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

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var idempotentAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<IdempotentAttribute>()
                .FirstOrDefault();
            if (DateTime.Now > NextExtractorRefresh) KeyExtractors = new ConcurrentDictionary<string, Func<object[], string>>();
            if (idempotentAttribute != null)
            {
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
                var idempotencyKey = keyExtractor(arguments);
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    context.HttpContext.Items["IdempotencyKey"] = idempotencyKey;

                    var isDuplicate = _idempotencyService.CheckForDuplicate(idempotencyKey);
                    if (isDuplicate)
                    {
                        var response = _idempotencyService.GetResponse(idempotencyKey, idempotentAttribute.TimeOut);
                        if (response?.Response == null) context.Result = new StatusCodeResult(StatusCodes.Status409Conflict);
                        else context.Result = response.Response;
                        return;
                    }
                    _idempotencyService.Upsert(idempotencyKey, window: idempotentAttribute.Window);

                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var idempotentAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<IdempotentAttribute>()
                .FirstOrDefault();

            if (idempotentAttribute != null)
            {

                var idempotencyKey = context.HttpContext.Items["IdempotencyKey"]?.ToString();
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    _idempotencyService.Upsert(idempotencyKey, context.Result, false, window: idempotentAttribute.Window);

                }
            }
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
    }

}
