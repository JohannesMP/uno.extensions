﻿namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseConfiguration(
		this IHostBuilder hostBuilder,
		Action<IConfigurationBuilder>? configureHostConfiguration = default,
		Action<HostBuilderContext, IConfigurationBuilder>? configureAppConfiguration = default,
		Func<IConfigBuilder, IHostBuilder>? configure = default)
	{
		if (configureHostConfiguration is not null)
		{
			hostBuilder = hostBuilder.ConfigureHostConfiguration(configureHostConfiguration);
		}

		if (configureAppConfiguration is not null)
		{
			hostBuilder = hostBuilder.ConfigureAppConfiguration(configureAppConfiguration);
		}

		hostBuilder = hostBuilder.ConfigureServices((ctx, s) =>
				{
					// We're doing the IsRegistered check here so that the
					// other configure delegates still run if required
					if (ctx.IsRegistered(nameof(UseConfiguration)))
					{
						return;
					}

					s.TryAddSingleton(a => ctx.Configuration);
					s.TryAddSingleton(a => (IConfigurationRoot)ctx.Configuration);
					s.TryAddSingleton<Reloader>();
					s.TryAddSingleton<ReloadService>();
					_ = s.AddHostedService(sp => sp.GetRequiredService<ReloadService>());
					s.TryAddSingleton<IStartupService>(sp => sp.GetRequiredService<ReloadService>());
				});
		hostBuilder = configure?.Invoke(hostBuilder.AsConfigBuilder()) ?? hostBuilder;

		return hostBuilder;
	}


	internal static IConfigBuilder AsConfigBuilder(this IHostBuilder hostBuilder)
	{
		if (hostBuilder is ConfigBuilder configBuilder)
		{
			return configBuilder;
		}

		return new ConfigBuilder(hostBuilder);
	}
}
