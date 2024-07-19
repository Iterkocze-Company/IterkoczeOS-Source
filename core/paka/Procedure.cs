public class Procedure  {
    public string Name {get; set;} = String.Empty;

    public string[] Value {get; set;} = Array.Empty<string>();
    public bool IsRequired {get; init;}
    public STATE State {get; set;} = STATE.UNITITIALISED;

    public enum STATE {
        UNITITIALISED,
        VALID,
        INVALID
    }
}