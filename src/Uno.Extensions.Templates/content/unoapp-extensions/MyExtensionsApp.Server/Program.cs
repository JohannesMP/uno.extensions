//+:cnd:noEmit
#if (use-serilog)
using Serilog;
#endif
#if wasm
using Uno.Wasm.Bootstrap.Server;
#endif

try
{
#if (use-serilog)
	Log.Logger = new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(Path.Combine("App_Data", "Logs", "log.txt"))
			.CreateLogger();
#endif
	var builder = WebApplication.CreateBuilder(args);
#if (use-serilog)
	builder.Host.UseSerilog();
#endif

	// Add services to the container.

	builder.Services.AddControllers();
	builder.Services.Configure<RouteOptions>(options =>
		options.LowercaseUrls = true);

	// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen(c =>
	{
		// Include XML comments for all included assemblies
		Directory.EnumerateFiles(AppContext.BaseDirectory, "*.xml")
			.Where(x => x.Contains("MyExtensionsApp")
				&& File.Exists(Path.Combine(
					AppContext.BaseDirectory,
					$"{Path.GetFileNameWithoutExtension(x)}.dll")))
			.ToList()
			.ForEach(path => c.IncludeXmlComments(path));
	});

	var app = builder.Build();

	// Configure the HTTP request pipeline.
	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	app.UseHttpsRedirection();

	app.UseAuthorization();
#if wasm

	app.UseUnoFrameworkFiles();
	app.MapFallbackToFile("index.html");
#endif

	app.MapControllers();
	app.UseStaticFiles();

	await app.RunAsync();
}
catch (Exception ex)
{
#if (use-serilog)
	Log.Fatal(ex, "Application terminated unexpectedly");
#else
	Console.Error.WriteLine("Application terminated unexpectedly");
	Console.Error.WriteLine(ex);
#endif
	if (System.Diagnostics.Debugger.IsAttached)
	{
		System.Diagnostics.Debugger.Break();
	}
}
#if (use-serilog)
finally
{
	Log.CloseAndFlush();
}
#endif
//-:cnd:noEmit
