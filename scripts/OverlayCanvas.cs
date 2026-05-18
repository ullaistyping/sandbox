using Godot;

// Thin CanvasItem that lets Main draw overlays on top of the TextureRect.
// Main creates this node programmatically (after TextureRect in the child list)
// so it renders above the simulation pixels.
public partial class OverlayCanvas : Control
{
	public System.Action<OverlayCanvas> OnDraw;
	public override void _Draw() => OnDraw?.Invoke(this);
}
