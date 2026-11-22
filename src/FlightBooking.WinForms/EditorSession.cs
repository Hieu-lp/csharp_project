namespace FlightBooking.WinForms;

internal enum EditorMode { View, New, Edit }

// A baseline distinguishes a real edit from merely opening a form or undoing typing.
internal sealed class EditorSession
{
    public EditorMode Mode { get; private set; }
    private string baseline = "";
    public void Begin(EditorMode mode, string snapshot) { Mode = mode; baseline = snapshot; }
    public bool IsDirty(string snapshot) => Mode != EditorMode.View && snapshot != baseline;
    public bool CanSave(string snapshot) => Mode == EditorMode.New || Mode == EditorMode.Edit && IsDirty(snapshot);
}
