namespace CS2_SimpleAdmin_PlayTimeModule;

public class PlayTimeModel
{
    public PlayTimeModel()
    {
    }

    public PlayTimeModel(int totalPlayTime)
    {
        TotalPlayTime = totalPlayTime;
    }

    public int TotalPlayTime { get; set; }
    public DateTime JoinedTime { get; set; }
    public int OldTeam { get; set; } = 1;
    
    public Dictionary<int, PlayerTeamModel> Teams { get; set; } = new()
    {
        { 0, new PlayerTeamModel() }, // Hidden
        { 1, new PlayerTeamModel() }, // Spec
        { 2, new PlayerTeamModel() }, // TT
        { 3, new PlayerTeamModel() }, // CT
    };
}

public class PlayerTeamModel
{
    public int PlayTime { get; set; }
    public DateTime? JoinedTime { get; set; }
}