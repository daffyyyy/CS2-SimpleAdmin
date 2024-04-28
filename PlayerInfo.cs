namespace CS2_SimpleAdmin
{
	public class PlayerInfo
	{
		public int? Index { get; set; }
		public int UserId { get; init; }
		public int Slot { get; init; }
		public string? SteamId { get; init; }
		public string? Name { get; init; }
		public string? IpAddress { get; init; }
	}
}