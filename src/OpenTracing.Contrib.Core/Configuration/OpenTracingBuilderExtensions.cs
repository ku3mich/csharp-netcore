using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.GenericListeners;
using OpenTracing.Contrib.NetCore.HttpHandler;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Contrib.NetCore.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        internal static IOpenTracingBuilder AddDiagnosticSubscriber<TDiagnosticSubscriber>(this IOpenTracingBuilder builder)
            where TDiagnosticSubscriber : DiagnosticObserver
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticObserver, TDiagnosticSubscriber>());

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<AspNetCoreDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(AspNetCoreDiagnostics.DiagnosticListenerName));

            // Our default behavior for ASP.NET is that we only want spans if the request itself is traced
            builder.ConfigureHttpHandler(opt => opt.StartRootSpans = false);

            return ConfigureAspNetCore(builder, options);
        }

        public static IOpenTracingBuilder ConfigureAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for System.Net.Http.
        /// </summary>
        public static IOpenTracingBuilder AddHttpHandler(this IOpenTracingBuilder builder, Action<HttpHandlerDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<HttpHandlerDiagnostics>();
            builder.ConfigureGenericDiagnostics(options => options.IgnoredListenerNames.Add(HttpHandlerDiagnostics.DiagnosticListenerName));

            return ConfigureHttpHandler(builder, options);
        }

        public static IOpenTracingBuilder ConfigureHttpHandler(this IOpenTracingBuilder builder, Action<HttpHandlerDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for generic DiagnosticListeners.
        /// </summary>
        public static IOpenTracingBuilder AddGenericDiagnostics(this IOpenTracingBuilder builder, Action<GenericDiagnosticOptions> options = null)
        {
            builder.AddDiagnosticSubscriber<GenericDiagnostics>();

            return ConfigureGenericDiagnostics(builder, options);
        }

        public static IOpenTracingBuilder ConfigureGenericDiagnostics(this IOpenTracingBuilder builder, Action<GenericDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Disables tracing for all diagnostic listeners that don't have an explicit implementation.
        /// </summary>
        public static IOpenTracingBuilder RemoveGenericDiagnostics(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.RemoveAll<GenericDiagnostics>();

            return builder;
        }

        public static IOpenTracingBuilder AddLoggerProvider(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OpenTracingLoggerProvider>());
            builder.Services.Configure<LoggerFilterOptions>(options =>
            {
                // All interesting request-specific logs are instrumented via DiagnosticSource.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.AspNetCore.Hosting", LogLevel.None);

                // The "Information"-level in ASP.NET Core is too verbose.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning);

                // EF Core is sending everything to DiagnosticSource AND ILogger so we completely disable the category.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.EntityFrameworkCore", LogLevel.None);
            });

            return builder;
        }
    }
}
