﻿using System.Collections.ObjectModel;
using System.Globalization;
using Prism.Behaviors;
using Prism.Extensions;
using Prism.Ioc;
using Prism.Properties;
using Prism.Regions.Adapters;
using XamlNavigation = Prism.Navigation.Xaml.Navigation;

namespace Prism.Regions.Behaviors;

/// <summary>
/// Behavior that creates a new <see cref="IRegion"/>, when the control that will host the <see cref="IRegion"/> (see <see cref="TargetElement"/>)
/// is added to the VisualTree. This behavior will use the <see cref="RegionAdapterMappings"/> class to find the right type of adapter to create
/// the region. After the region is created, this behavior will detach.
/// </summary>
/// <remarks>
/// Attached property value inheritance is not available in Silverlight, so the current approach walks up the visual tree when requesting a region from a region manager.
/// The <see cref="RegionManagerRegistrationBehavior"/> is now responsible for walking up the Tree.
/// </remarks>
public class DelayedRegionCreationBehavior
{
    private static readonly ICollection<DelayedRegionCreationBehavior> _instanceTracker =
        new Collection<DelayedRegionCreationBehavior>();

    private readonly RegionAdapterMappings _regionAdapterMappings;
    private readonly object _trackerLock = new object();

    private WeakReference _elementWeakReference;
    private bool _regionCreated = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayedRegionCreationBehavior"/> class.
    /// </summary>
    /// <param name="regionAdapterMappings">
    /// The region adapter mappings, that are used to find the correct adapter for
    /// a given controltype. The controltype is determined by the <see name="TargetElement"/> value.
    /// </param>
    public DelayedRegionCreationBehavior(RegionAdapterMappings regionAdapterMappings)
    {
        _regionAdapterMappings = regionAdapterMappings;
        RegionManagerAccessor = new DefaultRegionManagerAccessor();
    }

    /// <summary>
    /// Sets a class that interfaces between the <see cref="RegionManager"/> 's static properties/events and this behavior,
    /// so this behavior can be tested in isolation.
    /// </summary>
    /// <value>The region manager accessor.</value>
    public IRegionManagerAccessor RegionManagerAccessor { get; }

    /// <summary>
    /// The element that will host the Region.
    /// </summary>
    /// <value>The target element.</value>
    public VisualElement TargetElement
    {
        get => _elementWeakReference != null ? _elementWeakReference.Target as VisualElement : null;
        set => _elementWeakReference = new WeakReference(value);
    }

    private Page ParentPage => TargetElement.GetParentPage();

    /// <summary>
    /// Start monitoring the <see cref="RegionManager"/> and the <see cref="TargetElement"/> to detect when the <see cref="TargetElement"/> becomes
    /// part of the Visual Tree. When that happens, the Region will be created and the behavior will <see cref="Detach"/>.
    /// </summary>
    public void Attach()
    {
        RegionManagerAccessor.UpdatingRegions += OnUpdatingRegions;
        TargetElement.Behaviors.Add(new DelayedRegionCreationCallbackBehavior(TryCreateRegion));
    }

    /// <summary>
    /// Stop monitoring the <see cref="RegionManager"/> and the  <see cref="TargetElement"/>, so that this behavior can be garbage collected.
    /// </summary>
    public void Detach()
    {
        RegionManagerAccessor.UpdatingRegions -= OnUpdatingRegions;
        Untrack();
    }

    /// <summary>
    /// Called when the <see cref="IRegionManager"/> is updating it's <see cref="IRegionManager.Regions"/> collection.
    /// </summary>
    /// <param name="sender">The <see cref="IRegionManager"/>. </param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    public void OnUpdatingRegions(object sender, EventArgs e)
    {
        TryCreateRegion();
    }

    private void TryCreateRegion()
    {
        if (TargetElement == null)
        {
            Detach();
            return;
        }

        if (TargetElement.TryGetParentPage(out var _))
        {
            Detach();

            if (!_regionCreated)
            {
                string regionName = RegionManagerAccessor.GetRegionName(TargetElement);
                CreateRegion(TargetElement, regionName);
                Track();
                _regionCreated = true;
            }
        }
    }

    /// <summary>
    /// Method that will create the region, by calling the right <see cref="IRegionAdapter"/>.
    /// </summary>
    /// <param name="targetElement">The target element that will host the <see cref="IRegion"/>.</param>
    /// <param name="regionName">Name of the region.</param>
    /// <returns>The created <see cref="IRegion"/></returns>
    protected virtual IRegion CreateRegion(VisualElement targetElement, string regionName)
    {
        if (targetElement == null)
            throw new ArgumentNullException(nameof(targetElement));

        try
        {
            if (!targetElement.TryGetParentPage(out var page))
                throw new Exception("The Target Element has not yet been parented and we cannot get the parent page.");

            // Build the region
            var container = page.GetValue(XamlNavigation.NavigationScopeProperty) as IContainerProvider;
            targetElement.SetValue(XamlNavigation.NavigationScopeProperty, container);
            var regionAdapter = _regionAdapterMappings.GetMapping(targetElement.GetType());
            var region = regionAdapter.Initialize(targetElement, regionName);
            var cleanupBehavior = new RegionCleanupBehavior(region);

            page.Behaviors.Add(cleanupBehavior);

            return region;
        }
        catch (Exception ex)
        {
            throw new RegionCreationException(string.Format(CultureInfo.CurrentCulture, Resources.RegionCreationException, regionName, ex), ex);
        }
    }

    /// <summary>
    /// Add the instance of this class to <see cref="_instanceTracker"/> to keep it alive
    /// </summary>
    private void Track()
    {
        lock (_trackerLock)
        {
            if (!_instanceTracker.Contains(this))
            {
                _instanceTracker.Add(this);
            }
        }
    }

    /// <summary>
    /// Remove the instance of this class from <see cref="_instanceTracker"/>
    /// so it can eventually be garbage collected
    /// </summary>
    private void Untrack()
    {
        lock (_trackerLock)
        {
            _instanceTracker.Remove(this);
        }
    }
}
