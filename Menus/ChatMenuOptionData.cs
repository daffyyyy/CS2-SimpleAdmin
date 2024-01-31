namespace CS2_SimpleAdmin.Menus
{
	public class ChatMenuOptionData
	{
		public string name;
		public Action action;
		public bool disabled = true;

		public ChatMenuOptionData(string name, Action action)
		{
			this.name = name;
			this.action = action;
			this.disabled = false;
		}

		public ChatMenuOptionData(string name, Action action, bool disabled)
		{
			this.name = name;
			this.action = action;
			this.disabled = disabled;
		}
	}
}
