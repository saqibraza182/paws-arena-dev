using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using BoomDaoWrapper;

[Serializable]
public class PlayerData
{
    public const string EARNED_XP_KEY = "earnedXp";
    private float snacks;
    private float jugOfMilk;
    private float glassOfMilk;
    private CraftingProcess craftingProcess;
    private bool hasPass;
    private double experience;
    private int level;
    private int experienceOnCurrentLevel;
    private int experienceForNextLevel;
    private List<ClaimedReward> claimedLevelRewards = new ();
    private List<RecoveryEntrie> recoveringKitties = new ();
    private List<int> ownedEquiptables;
    private int seasonNumber;
    private List<int> ownedEmojis;
    private Challenges challenges = new ();
    private string guildId = string.Empty;
    private int points;

    public static Action OnUpdatedShards;
    [JsonIgnore] public Action UpdatedSnacks;
    [JsonIgnore] public Action UpdatedJugOfMilk;
    [JsonIgnore] public Action UpdatedGlassOfMilk;
    [JsonIgnore] public Action UpdatedCraftingProcess;
    [JsonIgnore] public Action UpdatedClaimedLevels;
    [JsonIgnore] public Action UpdatedHasPass;
    [JsonIgnore] public Action UpdatedExp;
    [JsonIgnore] public Action UpdatedRecoveringKitties;
    [JsonIgnore] public Action UpdatedEquiptables;
    [JsonIgnore] public Action UpdatedSeasonNumber;
    [JsonIgnore] public Action UpdatedOwnedEmojis;
    [JsonIgnore] public Action UpdatedGuild;
    [JsonIgnore] public Action UpdatedPoints;

    public void SetStartingValues()
    {
        ownedEquiptables = new List<int>() { 0, 25, 60, 74, 95 };
        ownedEmojis = new List<int>() {0,1,2,3,4};
    }

    public float Snacks
    {
        get { return snacks; }
        set
        {
            snacks = value;
            UpdatedSnacks?.Invoke();
        }
    }

    public float JugOfMilk
    {
        get { return jugOfMilk; }
        set
        {
            jugOfMilk = value;
            UpdatedJugOfMilk?.Invoke();
        }
    }

    public float GlassOfMilk
    {
        get { return glassOfMilk; }
        set
        {
            glassOfMilk = value;
            UpdatedGlassOfMilk?.Invoke();
        }

    }

    public CraftingProcess CraftingProcess
    {
        get { return craftingProcess; }
        set
        {
            craftingProcess = value;
            UpdatedCraftingProcess?.Invoke();
        }
    }

    public double Experience
    {
        get { return experience; }
        set
        {
            experience = value;
            CalculateLevel(experience,out level,out experienceForNextLevel,out experienceOnCurrentLevel);
            UpdatedExp?.Invoke();
        }
    }


    [JsonIgnore]
    public int Level
    {
        get { return level; }
    }

    public void AddCollectedLevelReward(ClaimedReward _reward)
    {
        claimedLevelRewards.Add(_reward);
        UpdatedClaimedLevels?.Invoke();
    }

    public List<ClaimedReward> ClaimedLevelRewards
    {
        get { return claimedLevelRewards; }
        set { claimedLevelRewards = value; }
    }

    [JsonIgnore] public int ExperienceOnCurrentLevel => experienceOnCurrentLevel;
    [JsonIgnore] public int ExperienceForNextLevel => experienceForNextLevel;

    public bool HasPass
    {
        get { return hasPass; }
        set
        {
            hasPass = value;
            UpdatedHasPass?.Invoke();
        }
    }

    public bool HasClaimed(LevelReward _reward, int _level)
    {
        foreach (var _claimedReward in claimedLevelRewards)
        {
            if (_claimedReward.IsPremium == _reward.IsPremium && _claimedReward.Level == _level)
            {
                return true;
            }
        }

        return false;
    }

    public List<RecoveryEntrie> RecoveringKitties
    {
        get { return recoveringKitties; }
    }

    public void AddRecoveringKittie(RecoveryEntrie _recoveryEntrie)
    {
        recoveringKitties.Add(_recoveryEntrie);
        UpdatedRecoveringKitties?.Invoke();
    }

    public void RemoveRecoveringKittie(string _imageUrl)
    {
        RecoveryEntrie _entry = null;

        foreach (var _recovery in recoveringKitties)
        {
            if (_recovery.KittyImageUrl == _imageUrl)
            {
                _entry = _recovery;
                break;
            }
        }

        if (_entry == null)
        {
            return;
        }

        recoveringKitties.Remove(_entry);
        UpdatedRecoveringKitties?.Invoke();
    }

    public List<int> OwnedEquiptables
    {
        get { return ownedEquiptables; }
        set { ownedEquiptables = value; }
    }

    public void AddOwnedEquipment(int _id)
    {
        if (ownedEquiptables.Contains(_id))
        {
            return;
        }

        ownedEquiptables.Add(_id);
        UpdatedEquiptables?.Invoke();
    }

    public void RemoveOwnedEquipment(int _id)
    {
        if (!ownedEquiptables.Contains(_id))
        {
            return;
        }

        ownedEquiptables.Remove(_id);
        UpdatedEquiptables?.Invoke();
    }

    public int SeasonNumber
    {
        get => seasonNumber;
        set
        {
            seasonNumber = value;
            UpdatedSeasonNumber?.Invoke();
        }
    }

    public List<int> OwnedEmojis
    {
        get => ownedEmojis;
        set
        {
            ownedEmojis = value;
            ownedEmojis.Sort();
        }
    }

    public void AddOwnedEmoji(int _id)
    {
        if (ownedEmojis.Contains(_id))
        {
            return;
        }

        ownedEmojis.Add(_id);
        ownedEmojis.Sort();
        UpdatedOwnedEmojis?.Invoke();
    }

    public Challenges Challenges
    {
        get => challenges;
        set => challenges = value;
    }


    public static void CalculateLevel(double _exp, out int level, out int expForNextLevel, out int experienceOnCurrentLevel)
    {
        double _experience = _exp;
        int _level = 1;
        float _expForNextLevel = DataManager.Instance.GameData.LevelBaseExp;

        if (_experience < DataManager.Instance.GameData.LevelBaseExp)
        {
            experienceOnCurrentLevel = (int)_experience;
            _expForNextLevel = DataManager.Instance.GameData.LevelBaseExp;
        }
        else
        {
            while (_experience >= _expForNextLevel)
            {
                _level++;
                _experience -= _expForNextLevel;
                _expForNextLevel = _expForNextLevel +
                                   (_expForNextLevel * ((float)DataManager.Instance.GameData.LevelBaseScaler / 100));
            }
        }

        expForNextLevel = (int)_expForNextLevel;
        experienceOnCurrentLevel = (int)_experience;
        level = _level;
    }

    public string GuildId
    {
        get => guildId;
        set
        {
            guildId = value;
            UpdatedGuild?.Invoke();
        }
    }

    [JsonIgnore] public string PlayerId => FirebaseManager.Instance.PlayerId;
    [JsonIgnore] public bool IsInGuild => !string.IsNullOrEmpty(GuildId);
    
    [JsonIgnore] public GuildData Guild
    {
        get
        {
            if (!IsInGuild)
            {
                return null;
            }

            GuildData _guild = null;
            try
            { 
                _guild = DataManager.Instance.GameData.Guilds[guildId];
            }
            catch
            {
                GuildId = string.Empty;
                return null;
            }
            if (_guild==null)
            {
                GuildId = string.Empty;
                return null;
            }
            bool _isStillInGuild = false;
            foreach (var _player in _guild.Players)
            {
                if (_player.Id==FirebaseManager.Instance.PlayerId)
                {
                    _isStillInGuild = true;
                }
            }
            

            if (!_isStillInGuild)
            {
                GuildId = string.Empty;
                return null;
            }
            
            _guild.ReorderPlayersByPoints();
            return _guild;
        }
    }

    public int Points
    {
        get => points;
        set
        {
            points = value;
            UpdatedPoints?.Invoke();
        }
    }
    
    public const string NAME_KEY = "username";
    private const string COMMON_SHARD = "commonShard";
    private const string UNCOMMON_SHARD = "uncommonShard";
    private const string RARE_SHARD = "rareShard";
    private const string EPIC_SHARD = "epicShard";
    private const string LEGENDARY_SHARD = "legendaryShard";
    private const string NAME_ENTITY_ID = "user_profile";
    private const string VALUABLES_ENTITY_ID = "user_valuables";

    public string Username => BoomDaoUtility.Instance.GetString(NAME_ENTITY_ID, NAME_KEY);
    public double CommonShard => BoomDaoUtility.Instance.GetDouble(VALUABLES_ENTITY_ID, COMMON_SHARD);
    public double UncommonShard => BoomDaoUtility.Instance.GetDouble(VALUABLES_ENTITY_ID, UNCOMMON_SHARD);
    public double RareShard => BoomDaoUtility.Instance.GetDouble(VALUABLES_ENTITY_ID, RARE_SHARD);
    public double EpicShard => BoomDaoUtility.Instance.GetDouble(VALUABLES_ENTITY_ID, EPIC_SHARD);
    public double LegendaryShard => BoomDaoUtility.Instance.GetDouble(VALUABLES_ENTITY_ID, LEGENDARY_SHARD);
    public double TotalCrystals => BoomDaoUtility.Instance.GetDouble(VALUABLES_ENTITY_ID, LEGENDARY_SHARD);

    public double GetAmountOfCrystals(LuckyWheelRewardType _type)
    {
        switch (_type)
        {
            case LuckyWheelRewardType.Common:
                return CommonShard;
            case LuckyWheelRewardType.Uncommon:
                return UncommonShard;
            case LuckyWheelRewardType.Rare:
                return RareShard;
            case LuckyWheelRewardType.Epic:
                return EpicShard;
            case LuckyWheelRewardType.Legendary:
                return LegendaryShard;
        }

        throw new Exception("Unsupported type of shards: " + _type);
    }
}
