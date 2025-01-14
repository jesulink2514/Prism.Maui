﻿using MauiRegionsModule.ViewModels;
using MauiRegionsModule.Views;

namespace MauiRegionsModule;

public class MauiTestRegionsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        //var regionManager = containerProvider.Resolve<IRegionManager>();
        //regionManager.RegisterViewWithRegion("ContentRegion", "RegionViewA");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry
            .RegisterForNavigation<ContentRegionPage>()
            .RegisterForNavigation<RegionHome, RegionHomeViewModel>()
            .RegisterForRegionNavigation<RegionViewA, RegionViewAViewModel>()
            .RegisterForRegionNavigation<RegionViewB, RegionViewBViewModel>()
            .RegisterForRegionNavigation<RegionViewC, RegionViewCViewModel>();
    }
}