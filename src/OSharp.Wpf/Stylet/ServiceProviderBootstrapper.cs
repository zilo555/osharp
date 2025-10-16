// -----------------------------------------------------------------------
//  <copyright file="ServiceProviderBootstrapper.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2020 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2020-05-28 15:00</last-date>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;


namespace OSharp.Wpf.Stylet;

public abstract class ServiceProviderBootstrapper<TRootViewModel> : BootstrapperBase where TRootViewModel : class
{
    private HostApplicationBuilder _hostBuilder;
    private IHost _host;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private TRootViewModel _rootViewModel;
    protected virtual TRootViewModel RootViewModel => _rootViewModel ??= (TRootViewModel)GetInstance(typeof(TRootViewModel));

    protected IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Called on application startup. This occur after this.Args has been assigned, but before the IoC container has been configured
    /// </summary>
    protected override void OnStart()
    {
        _hostBuilder = Host.CreateApplicationBuilder();
        _hostBuilder.Environment.EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    }

    /// <summary>
    /// Overridden from BootstrapperBase, this sets up the IoC container
    /// </summary>
    protected override void ConfigureBootstrapper()
    {
        _hostBuilder.Services.AddSingleton<IHostApplicationBuilder>(_hostBuilder);
        DefaultConfigureIoC(_hostBuilder.Services);
        ConfigureIoC(_hostBuilder.Services);

        _host = _hostBuilder.Build();
        ServiceProvider = _host.Services;
        _host.StartAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
    }

    protected virtual void ConfigureIoC(IServiceCollection services)
    { }

    protected virtual void DefaultConfigureIoC(IServiceCollection services)
    {
        var viewManagerConfig = new ViewManagerConfig()
        {
            ViewFactory = GetInstance,
            ViewAssemblies = [GetType().Assembly]
        };

        services.AddSingleton<IViewManager>(new ViewManager(viewManagerConfig));
        services.AddTransient<MessageBoxView>();

        services.AddSingleton<IWindowManagerConfig>(this);
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddTransient<IMessageBoxViewModel, MessageBoxViewModel>(); // Not singleton!
        // Also need a factory
        services.AddSingleton<Func<IMessageBoxViewModel>>(() => new MessageBoxViewModel());
    }

    /// <summary>
    /// Called when the application is launched. Should display the root view using <see cref="M:Stylet.BootstrapperBase.DisplayRootView(System.Object)" />
    /// </summary>
    protected override void Launch()
    {
        base.DisplayRootView(RootViewModel);
    }

    /// <summary>
    /// Given a type, use the IoC container to fetch an instance of it
    /// </summary>
    /// <param name="type">Type of instance to fetch</param>
    /// <returns>Fetched instance</returns>
    public override object GetInstance(Type type)
    {
        return ServiceProvider?.GetService(type);
    }

    /// <summary>Hook called on application exit</summary>
    /// <param name="e">The exit event data</param>
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        // 在应用程序退出时停止 host
        _cancellationTokenSource.Cancel();
        try
        {
            _host?.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // 记录日志但不阻止退出
            Debug.WriteLine($"Error stopping host during exit: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        try
        {
            _host?.StopAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _host?.Dispose();
            _cancellationTokenSource.Dispose();
        }
        ScreenExtensions.TryDispose(_rootViewModel);
    }
}
