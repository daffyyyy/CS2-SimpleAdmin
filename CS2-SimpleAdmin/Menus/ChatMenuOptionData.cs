namespace CS2_SimpleAdmin.Menus;

public class ChatMenuOptionData(string name, Action action, bool disabled = false)
{
    public readonly string Name = name;
    public readonly Action Action = action;
    public readonly bool Disabled = disabled;
}