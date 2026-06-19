using Terminal.Gui;

namespace NeonTrader.Core.Interfaces;

/// <summary>
/// Interface for UI components that can be rendered to the terminal.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// The root view for this renderable component
    /// </summary>
    View View { get; }

    /// <summary>
    /// Called when the component should refresh its display
    /// </summary>
    void Refresh();

    /// <summary>
    /// Called when the terminal size changes
    /// </summary>
    /// <param name="width">New terminal width</param>
    /// <param name="height">New terminal height</param>
    void OnResize(int width, int height);

    /// <summary>
    /// Z-index for rendering order (higher = on top)
    /// </summary>
    int ZIndex { get; }

    /// <summary>
    /// Whether this component is currently visible
    /// </summary>
    bool IsVisible { get; set; }
}