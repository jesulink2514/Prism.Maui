﻿using MauiModule;
using Prism;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation;
using PrismMauiDemo.Views;

namespace PrismMauiDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        return MauiApp.CreateBuilder()
            .UsePrismApp<App>()
            .ConfigureModuleCatalog(moduleCatalog =>
            {
                moduleCatalog.AddModule<MauiAppModule>();
            })
            .RegisterTypes(containerRegistry =>
            {
                containerRegistry.RegisterForNavigation<MainPage>();
                containerRegistry.RegisterForNavigation<RootPage>();
                containerRegistry.RegisterForNavigation<SamplePage>();
            })
            .OnAppStart(async navigationService =>
            {
                var result = await navigationService.NavigateAsync("MainPage/NavigationPage/ViewA/ViewB/ViewC/ViewD");
                if (!result.Success)
                {
                    System.Diagnostics.Debugger.Break();
                }
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            .Build();
    }
}
