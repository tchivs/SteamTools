using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;

namespace BD.WTTS.Services.Implementation;

public class NavigationService
{
    public static NavigationService Instance { get; } = new NavigationService();

    public Control? PreviousPage { get; set; }

    public void SetFrame(Frame f)
    {
        _frame = f;
    }

    public void SetOverlayHost(Panel p)
    {
        _overlayHost = p;
    }

    public void Navigate(Type t)
    {
        if (_frame?.Content?.GetType() != t)
        {
            _frame?.Navigate(t);
        }
    }

    public void Navigate(Type t, NavigationTransitionInfo? transitionInfo = null)
    {
        if (_frame?.Content?.GetType() != t)
        {
            _frame?.Navigate(t, null, transitionInfo ?? new SuppressNavigationTransitionInfo());
        }
    }

    public void GoBack()
    {
        _frame?.GoBack();
    }

    public void NavigateFromContext(object dataContext, NavigationTransitionInfo? transitionInfo = null)
    {
        if ((_frame?.Content as Control)?.DataContext != dataContext)
        {
            _frame?.NavigateFromObject(dataContext,
            new FluentAvalonia.UI.Navigation.FrameNavigationOptions
            {
                IsNavigationStackEnabled = true,
                TransitionInfoOverride = transitionInfo ?? new SuppressNavigationTransitionInfo()
            });
        }
    }

    public void ShowControlDefinitionOverlay(Type targetType)
    {
        if (_overlayHost != null)
        {

        }
    }

    public void ClearOverlay()
    {
        _overlayHost?.Children.Clear();

    }

    private Frame? _frame;
    private Panel? _overlayHost;
}


