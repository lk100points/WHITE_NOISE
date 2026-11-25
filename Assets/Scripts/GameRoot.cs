using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteNoise
{
    public sealed class GameRoot : MonoBehaviour
    {
        private enum UIMode { Title, Game, Pause, Settings, Gear, Dialogue, Ending }
        private enum GearKit { FishingKit, WorkGloves, TrailPack, JournalLantern }

        [Serializable]
        private sealed class SaveData
        {
            public int day;
            public float clockMinutes;
            public int money;
            public int fuel;
            public int food;
            public int firewood;
            public int fish;
            public int venison;
            public int cookedMeals;
            public int clues;
            public int trust;
            public bool workedToday;
            public bool stoveLit;
            public bool radioHeard;
            public bool cellarNoteFound;
            public bool nightEventSeen;
            public bool porchConversation;
            public bool deputyCalled;
            public bool deputyInterviewed;
            public string gear;
            public float playerX;
            public float playerY;
            public float playerZ;
            public float playerYaw;
        }

        private static GameRoot instance;
        private PlayerController player;
        private WorldPoint[] worldPoints;
        private UIMode mode = UIMode.Title;
        private GearKit selectedGear = GearKit.JournalLantern;
        private int day = 1;
        private float clockMinutes = 8f * 60f;
        private int money = 26;
        private int fuel = 3;
        private int food = 1;
        private int firewood;
        private int fish;
        private int venison;
        private int cookedMeals;
        private int clues;
        private int trust;
        private bool workedToday;
        private bool stoveLit;
        private bool radioHeard;
        private bool cellarNoteFound;
        private bool nightEventSeen;
        private bool porchConversation;
        private bool deputyCalled;
        private bool deputyInterviewed;
        private bool hasSave;
        private string currentMessage = "The snow has been falling since dawn.";
        private float messageTimer = 6f;
        private string currentLocation = "CABIN / EXTERIOR";
        private WorldPoint nearestPoint;
        private string dialogueTitle;
        private string dialogueText;
        private string[] dialogueOptions;
        private string dialogueKind;
        private string waitingForBinding;
        private AudioSource windSource;
        private AudioSource fireSource;
        private AudioClip windClip;
        private AudioClip fireClip;
        private bool pausedBeforeSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (FindObjectOfType<GameRoot>() == null)
            {
                GameObject root = new GameObject("WHITE NOISE / Runtime");
                root.AddComponent<GameRoot>();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            QualitySettings.antiAliasing = 2;
            QualitySettings.vSyncCount = 1;
            player = WorldBuilder.Build(this);
            LoadBindings();
            worldPoints = FindObjectsOfType<WorldPoint>();
            SetupAudio();
            hasSave = PlayerPrefs.HasKey("WHITE_NOISE_SAVE_V1");
            mode = UIMode.Title;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetupAudio()
        {
            windSource = gameObject.AddComponent<AudioSource>();
            windClip = MakeNoiseClip("Cold coast wind", 5f, 22050, 0.055f, 0.18f);
            windSource.clip = windClip;
            windSource.loop = true;
            windSource.volume = 0.45f;
            windSource.spatialBlend = 0f;
            windSource.Play();

            fireSource = gameObject.AddComponent<AudioSource>();
            fireClip = MakeNoiseClip("Wood stove crackle", 1.4f, 22050, 0.12f, 0.65f);
            fireSource.clip = fireClip;
            fireSource.loop = true;
            fireSource.volume = 0f;
            fireSource.spatialBlend = 0f;
            fireSource.Play();
        }

        private AudioClip MakeNoiseClip(string clipName, float seconds, int rate, float amplitude, float modulation)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] samples = new float[count];
            System.Random random = new System.Random(2004 + clipName.Length);
            for (int i = 0; i < count; i++)
            {
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                float slow = Mathf.Sin(i * 0.00017f) * 0.25f + Mathf.Sin(i * 0.000047f) * 0.20f + 0.55f;
                float crack = 0f;
                if (clipName.Contains("crackle") && random.NextDouble() > 0.996)
                    crack = (float)(random.NextDouble() * 2.0 - 1.0) * 0.8f;
                samples[i] = Mathf.Clamp((n * modulation + crack) * amplitude * slow, -0.9f, 0.9f);
            }
            AudioClip clip = AudioClip.Create(clipName, count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Update()
        {
            if (player == null) return;
            bool allowInput = mode == UIMode.Game;
            player.SetInputEnabled(allowInput);

            if (mode == UIMode.Game)
            {
                clockMinutes += Time.deltaTime * 2.0f;
                if (clockMinutes >= 24f * 60f) clockMinutes -= 24f * 60f;
                CheckNightEvent();
                if (Input.GetKeyDown(player.PauseKey))
                    mode = UIMode.Pause;
                else if (Input.GetKeyDown(player.InteractKey))
                    InteractNearest();
            }
            else if (mode == UIMode.Gear)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseGear(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseGear(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) ChooseGear(2);
                if (Input.GetKeyDown(KeyCode.Alpha4)) ChooseGear(3);
                if (Input.GetKeyDown(KeyCode.Escape)) mode = UIMode.Game;
            }
            else if (mode == UIMode.Dialogue)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseDialogue(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseDialogue(1);
                if (Input.GetKeyDown(KeyCode.Alpha3) && dialogueOptions != null && dialogueOptions.Length > 2) ChooseDialogue(2);
            }
            else if (mode == UIMode.Settings && !string.IsNullOrEmpty(waitingForBinding))
            {
                CaptureBinding();
            }

            if (messageTimer > 0f) messageTimer -= Time.deltaTime;
            UpdateLocation();
            if (windSource != null) windSource.volume = currentLocation == "CABIN / INTERIOR" ? 0.14f : 0.48f;
            if (fireSource != null) fireSource.volume = stoveLit && currentLocation == "CABIN / INTERIOR" ? 0.16f : 0f;
        }

        private void UpdateLocation()
        {
            Vector3 p = player.transform.position;
            if (p.x > 400f) currentLocation = "CABIN / INTERIOR";
            else if (p.z < 18f && p.x > -12f && p.x < 12f) currentLocation = "CABIN / EXTERIOR";
            else if (p.x < -35f && p.z < 20f) currentLocation = "THE CLIFF";
            else if (p.z < 90f) currentLocation = "THE FOREST";
            else if (p.z < 119f && Mathf.Abs(p.x) < 35f) currentLocation = "PENOBSCOT LUMBER";
            else if (p.z < 153f && p.x > 10f) currentLocation = "NORTH POINT GAS";
            else currentLocation = "NORTH POINT";

            nearestPoint = null;
            float nearest = 9999f;
            for (int i = 0; i < worldPoints.Length; i++)
            {
                WorldPoint point = worldPoints[i];
                if (point == null || !IsPointAvailable(point.actionId)) continue;
                float d = Vector3.Distance(p, point.transform.position);
                if (d <= point.range && d < nearest)
                {
                    nearest = d;
                    nearestPoint = point;
                }
            }
        }

        private bool IsPointAvailable(string action)
        {
            if (action == "deputy") return day >= 2 && nightEventSeen && !deputyInterviewed;
            if (action == "cellar_note") return !cellarNoteFound;
            if (action == "work") return !workedToday;
            if (action == "interior_exit") return currentLocation == "CABIN / INTERIOR";
            if (action == "gear_cabinet" || action == "stove" || action == "radio" || action == "cellar_note" || action == "bed" || action == "cook" || action == "inside_window")
                return currentLocation == "CABIN / INTERIOR";
            if (action == "cabin_door") return currentLocation == "CABIN / EXTERIOR";
            if (action == "eli") return day >= 1;
            return true;
        }

        private void InteractNearest()
        {
            if (nearestPoint == null)
            {
                SetMessage("Nothing close enough to interact with.");
                return;
            }
            switch (nearestPoint.actionId)
            {
                case "cabin_door":
                    player.Teleport(new Vector3(500f, 0.3f, 500f));
                    player.Face(0f);
                    SetMessage("The cabin is warmer than the world outside.");
                    break;
                case "interior_exit":
                    player.Teleport(new Vector3(0f, 0.05f, 9f));
                    player.Face(0f);
                    SetMessage("The cold comes straight through your coat.");
                    break;
                case "gear_cabinet":
                    mode = UIMode.Gear;
                    break;
                case "wood":
                    int logs = selectedGear == GearKit.WorkGloves ? 3 : 2;
                    firewood = Mathf.Min(12, firewood + logs);
                    AdvanceActionTime(35f);
                    SetMessage("You split and stack " + logs + " pieces of dry wood. Firewood: " + firewood + ".");
                    SaveGame();
                    break;
                case "tracks":
                    int meat = selectedGear == GearKit.TrailPack ? 2 : 1;
                    venison = Mathf.Min(8, venison + meat);
                    AdvanceActionTime(55f);
                    SetMessage("You return from the trail with provisions. Venison: " + venison + ". The hunt itself stays off-screen.");
                    SaveGame();
                    break;
                case "fish":
                    int catchCount = selectedGear == GearKit.FishingKit ? 2 : 1;
                    fish = Mathf.Min(8, fish + catchCount);
                    AdvanceActionTime(30f);
                    SetMessage("A quiet hour by the water. Fish: " + fish + ". The sea is almost silent.");
                    SaveGame();
                    break;
                case "work":
                    workedToday = true;
                    money += 36;
                    AdvanceActionTime(180f);
                    SetMessage("A shift at the mill. +$36. The foreman barely looks up.");
                    SaveGame();
                    break;
                case "gas":
                    if (money >= 12 && fuel < 20)
                    {
                        money -= 12;
                        fuel = Mathf.Min(20, fuel + 5);
                        AdvanceActionTime(10f);
                        SetMessage("Five gallons added. Fuel: " + fuel + ".");
                        SaveGame();
                    }
                    else SetMessage(money < 12 ? "You do not have enough money for fuel." : "The tank is already full enough.");
                    break;
                case "store":
                    if (money >= 9)
                    {
                        money -= 9;
                        food += 2;
                        AdvanceActionTime(15f);
                        SetMessage("A paper bag, two cans, a loaf of bread. Supplies: " + food + ".");
                        SaveGame();
                    }
                    else SetMessage("You count your cash twice. Still not enough.");
                    break;
                case "cook":
                    if (fish > 0 || venison > 0 || food > 0)
                    {
                        if (fish > 0) fish--;
                        else if (venison > 0) venison--;
                        else food--;
                        cookedMeals++;
                        AdvanceActionTime(25f);
                        SetMessage("Dinner is simple and hot. For a little while, the house feels ordinary.");
                        SaveGame();
                    }
                    else SetMessage("The kitchen is empty. You should stop at the store.");
                    break;
                case "stove":
                    if (firewood > 0)
                    {
                        firewood--;
                        stoveLit = true;
                        AdvanceActionTime(8f);
                        SetMessage("The stove catches. A soft orange light settles over the room.");
                        SaveGame();
                    }
                    else SetMessage("No dry wood left. The stove stays cold.");
                    break;
                case "radio":
                    radioHeard = true;
                    clues = Mathf.Min(8, clues + 1);
                    OpenDialogue("RADIO / 8:14 PM", "A voice breaks through the static: 'North Point ferry service remains suspended. Anyone with information about the December 1998 crossing should contact the sheriff.' Then the station slips back into music.", new string[] { "Write it in the journal", "Turn the radio off" }, "radio");
                    SaveGame();
                    break;
                case "cellar_note":
                    cellarNoteFound = true;
                    clues++;
                    OpenDialogue("THE CELLAR DRAWER", "Inside the drawer: an old ferry ticket, a photograph with its face turned to the wall, and a name written in blue ink. SAMUEL VALE. The ticket is dated December 1998. You have been here before.", new string[] { "Keep reading", "Close the drawer" }, "cellar");
                    SaveGame();
                    break;
                case "bed":
                    RestForNextDay();
                    break;
                case "inside_window":
                    OpenDialogue("THE WINDOW", nightEventSeen ? "A dark vehicle sits far down the road. Its lights go out before you can make out the plate." : "The snow keeps falling. Beyond the porch, the tree line looks almost black.", new string[] { "Step away from the window", "Write down what you saw" }, "window");
                    break;
                case "eli":
                    OpenDialogue("ELI MERCER", "'You picked a strange winter to come back, Sam. The old ferry road is closed. Has been for years. People here still remember that night.' He waits for your answer.", new string[] { "Ask what happened in 1998", "Talk about the weather" }, "eli");
                    break;
                case "mara":
                    OpenDialogue("MARA / NORTH POINT GAS", "A traveller in a red coat warms her hands around a paper cup. 'You live up by the cliff? I saw a light in that cabin last night. Thought nobody used it anymore.'", new string[] { "Ask what she saw", "Wish her a safe drive" }, "mara");
                    break;
                case "deputy":
                    OpenDialogue("DEPUTY REED", "'I am following up on a call about the road near your place. No emergency. I only need to know whether you heard or saw anything unusual last night.'", new string[] { "Tell him about the radio and tracks", "Ask about the 1998 ferry report" }, "deputy");
                    break;
                case "town_board":
                    clues++;
                    OpenDialogue("NORTH POINT / NOTICE BOARD", "A faded notice from December 1998 is pinned beneath a newer snow-emergency warning. 'FERRY INCIDENT REVIEW — public meeting postponed.' Someone has circled the date in pencil.", new string[] { "Take a note", "Leave it there" }, "notice");
                    break;
            }
        }

        private void CheckNightEvent()
        {
            if (day == 1 && !nightEventSeen && clockMinutes >= 19.5f * 60f)
            {
                nightEventSeen = true;
                OpenDialogue("THE LAST WINTER", "The radio cuts to silence. Three knocks come from the porch. Through the frosted glass you see no one, only a fresh line of tire tracks ending at your steps. The house is warm. The road outside is empty.", new string[] { "Turn on the porch light and speak", "Call Deputy Reed", "Write down the time and wait for daylight" }, "night");
                SaveGame();
            }
        }

        private void AdvanceActionTime(float minutes)
        {
            clockMinutes += minutes;
            CheckNightEvent();
        }

        private void OpenDialogue(string title, string body, string[] options, string kind)
        {
            dialogueTitle = title;
            dialogueText = body;
            dialogueOptions = options;
            dialogueKind = kind;
            mode = UIMode.Dialogue;
        }

        private void ChooseDialogue(int choice)
        {
            if (dialogueOptions == null || choice < 0 || choice >= dialogueOptions.Length) return;
            switch (dialogueKind)
            {
                case "night":
                    if (choice == 0)
                    {
                        porchConversation = true;
                        trust++;
                        clues++;
                        SetMessage("A voice from the road answers: 'You remember me.' Then the snow swallows the sound.");
                    }
                    else if (choice == 1)
                    {
                        deputyCalled = true;
                        trust += 2;
                        SetMessage("You report the tracks. Deputy Reed says he will visit in the morning.");
                    }
                    else
                    {
                        clues++;
                        SetMessage("You record the exact time. Outside, the tracks begin to fill with snow.");
                    }
                    SaveGame();
                    mode = UIMode.Game;
                    break;
                case "eli":
                    if (choice == 0)
                    {
                        clues++;
                        SetMessage("Eli says the ferry disappeared from the local schedule after the crossing in 1998.");
                    }
                    else
                    {
                        trust++;
                        SetMessage("Eli talks about the weather, but keeps looking toward the old road.");
                    }
                    AdvanceActionTime(20f);
                    mode = UIMode.Game;
                    break;
                case "mara":
                    if (choice == 0)
                    {
                        clues++;
                        SetMessage("Mara remembers a second set of headlights near the cabin. She is not sure when.");
                    }
                    else
                    {
                        trust++;
                        SetMessage("Mara leaves with a wave. Her taillights disappear into the grey road.");
                    }
                    AdvanceActionTime(15f);
                    mode = UIMode.Game;
                    break;
                case "deputy":
                    deputyInterviewed = true;
                    if (choice == 0)
                    {
                        trust += 2;
                        clues++;
                        SetMessage("Reed writes down the details. 'That helps. If you remember anything else, call me.'");
                    }
                    else
                    {
                        clues++;
                        SetMessage("Reed confirms the ferry review was never formally closed. He leaves you a case number.");
                    }
                    AdvanceActionTime(25f);
                    SaveGame();
                    mode = UIMode.Game;
                    break;
                case "radio":
                case "cellar":
                case "window":
                case "notice":
                    if (choice == 0) clues++;
                    SetMessage("You add another line to the notebook. The handwriting looks older than you remember.");
                    mode = UIMode.Game;
                    SaveGame();
                    break;
                default:
                    mode = UIMode.Game;
                    break;
            }
        }

        private void RestForNextDay()
        {
            if (clockMinutes < 18f * 60f)
            {
                SetMessage("It is too early to sleep. The day has not finished with you yet.");
                return;
            }
            if (day >= 6)
            {
                ShowEnding();
                return;
            }
            day++;
            clockMinutes = 7.5f * 60f;
            workedToday = false;
            stoveLit = false;
            deputyInterviewed = false;
            selectedGear = GearKit.JournalLantern;
            SetMessage(day == 2 && (deputyCalled || porchConversation)
                ? "A new day. There is a message from Deputy Reed on the answering machine."
                : "A new day. The wind has changed direction.");
            SaveGame();
        }

        private void ChooseGear(int index)
        {
            selectedGear = (GearKit)Mathf.Clamp(index, 0, 3);
            string[] names = { "Fishing kit", "Work gloves", "Trail pack", "Journal + lantern" };
            SetMessage("Morning kit selected: " + names[index] + ".");
            mode = UIMode.Game;
            SaveGame();
        }

        private void ShowEnding()
        {
            mode = UIMode.Ending;
            string ending;
            if (trust >= 4 && clues >= 5) ending = "THE RECORD IS COMPLETE\nYou give the sheriff enough pieces to reopen the ferry inquiry. North Point does not forgive the past, but it finally has a name for it.";
            else if (clues >= 3) ending = "THE WINTER FILE\nYou leave North Point with a notebook full of dates and a question you cannot answer. The coast keeps its silence.";
            else ending = "WHITE NOISE\nThe cabin is quiet again. The radio keeps playing. You decide not to ask what the town remembers.";
            dialogueTitle = "END OF THE VERTICAL SLICE";
            dialogueText = ending;
        }

        private void SetMessage(string message)
        {
            currentMessage = message;
            messageTimer = 7.0f;
        }

        private void StartNewGame()
        {
            day = 1;
            clockMinutes = 8f * 60f;
            money = 26;
            fuel = 3;
            food = 1;
            firewood = 0;
            fish = 0;
            venison = 0;
            cookedMeals = 0;
            clues = 0;
            trust = 0;
            workedToday = false;
            stoveLit = false;
            radioHeard = false;
            cellarNoteFound = false;
            nightEventSeen = false;
            porchConversation = false;
            deputyCalled = false;
            deputyInterviewed = false;
            selectedGear = GearKit.JournalLantern;
            player.Teleport(new Vector3(0f, 0.05f, 9f));
            player.Face(180f);
            mode = UIMode.Game;
            SetMessage("December 2004. Day 1. The road to the mill is already covered in snow.");
            SaveGame();
        }

        private void SaveGame()
        {
            SaveData data = new SaveData();
            data.day = day;
            data.clockMinutes = clockMinutes;
            data.money = money;
            data.fuel = fuel;
            data.food = food;
            data.firewood = firewood;
            data.fish = fish;
            data.venison = venison;
            data.cookedMeals = cookedMeals;
            data.clues = clues;
            data.trust = trust;
            data.workedToday = workedToday;
            data.stoveLit = stoveLit;
            data.radioHeard = radioHeard;
            data.cellarNoteFound = cellarNoteFound;
            data.nightEventSeen = nightEventSeen;
            data.porchConversation = porchConversation;
            data.deputyCalled = deputyCalled;
            data.deputyInterviewed = deputyInterviewed;
            data.gear = selectedGear.ToString();
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
            data.playerZ = player.transform.position.z;
            data.playerYaw = player.transform.eulerAngles.y;
            PlayerPrefs.SetString("WHITE_NOISE_SAVE_V1", JsonUtility.ToJson(data));
            PlayerPrefs.SetInt("WHITE_NOISE_F", (int)player.forwardKey);
            PlayerPrefs.SetInt("WHITE_NOISE_B", (int)player.backKey);
            PlayerPrefs.SetInt("WHITE_NOISE_L", (int)player.leftKey);
            PlayerPrefs.SetInt("WHITE_NOISE_R", (int)player.rightKey);
            PlayerPrefs.SetInt("WHITE_NOISE_S", (int)player.sprintKey);
            PlayerPrefs.SetInt("WHITE_NOISE_E", (int)player.interactKey);
            PlayerPrefs.SetInt("WHITE_NOISE_C", (int)player.cameraKey);
            PlayerPrefs.SetInt("WHITE_NOISE_P", (int)player.pauseKey);
            PlayerPrefs.Save();
            hasSave = true;
        }

        private void SaveBindings()
        {
            PlayerPrefs.SetInt("WHITE_NOISE_F", (int)player.forwardKey);
            PlayerPrefs.SetInt("WHITE_NOISE_B", (int)player.backKey);
            PlayerPrefs.SetInt("WHITE_NOISE_L", (int)player.leftKey);
            PlayerPrefs.SetInt("WHITE_NOISE_R", (int)player.rightKey);
            PlayerPrefs.SetInt("WHITE_NOISE_S", (int)player.sprintKey);
            PlayerPrefs.SetInt("WHITE_NOISE_E", (int)player.interactKey);
            PlayerPrefs.SetInt("WHITE_NOISE_C", (int)player.cameraKey);
            PlayerPrefs.SetInt("WHITE_NOISE_P", (int)player.pauseKey);
            PlayerPrefs.Save();
        }

        private void LoadBindings()
        {
            if (!PlayerPrefs.HasKey("WHITE_NOISE_F")) return;
            player.SetBindings(
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_F", (int)KeyCode.W),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_B", (int)KeyCode.S),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_L", (int)KeyCode.A),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_R", (int)KeyCode.D),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_S", (int)KeyCode.LeftShift),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_E", (int)KeyCode.E),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_C", (int)KeyCode.C),
                (KeyCode)PlayerPrefs.GetInt("WHITE_NOISE_P", (int)KeyCode.Escape));
        }

        private void LoadGame()
        {
            if (!PlayerPrefs.HasKey("WHITE_NOISE_SAVE_V1"))
            {
                SetMessage("No save file found.");
                return;
            }
            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("WHITE_NOISE_SAVE_V1"));
            if (data == null)
            {
                SetMessage("The save file could not be read.");
                return;
            }
            day = data.day;
            clockMinutes = data.clockMinutes;
            money = data.money;
            fuel = data.fuel;
            food = data.food;
            firewood = data.firewood;
            fish = data.fish;
            venison = data.venison;
            cookedMeals = data.cookedMeals;
            clues = data.clues;
            trust = data.trust;
            workedToday = data.workedToday;
            stoveLit = data.stoveLit;
            radioHeard = data.radioHeard;
            cellarNoteFound = data.cellarNoteFound;
            nightEventSeen = data.nightEventSeen;
            porchConversation = data.porchConversation;
            deputyCalled = data.deputyCalled;
            deputyInterviewed = data.deputyInterviewed;
            GearKit parsed;
            if (Enum.TryParse(data.gear, out parsed)) selectedGear = parsed;
            Vector3 savedPosition = new Vector3(data.playerX, data.playerY, data.playerZ);
            if (savedPosition.sqrMagnitude < 0.01f)
                savedPosition = day > 1 ? new Vector3(500f, 0.3f, 500f) : new Vector3(0f, 0.05f, 9f);
            player.Teleport(savedPosition);
            player.Face(data.playerYaw);
            mode = UIMode.Game;
            SetMessage("Save loaded. The radio is still on.");
        }

        private void BeginRebind(string binding)
        {
            waitingForBinding = binding;
        }

        private void CaptureBinding()
        {
            Array values = Enum.GetValues(typeof(KeyCode));
            foreach (KeyCode key in values)
            {
                if (key == KeyCode.None || key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2) continue;
                if (!Input.GetKeyDown(key)) continue;
                switch (waitingForBinding)
                {
                    case "forward": player.forwardKey = key; break;
                    case "back": player.backKey = key; break;
                    case "left": player.leftKey = key; break;
                    case "right": player.rightKey = key; break;
                    case "sprint": player.sprintKey = key; break;
                    case "interact": player.interactKey = key; break;
                    case "camera": player.cameraKey = key; break;
                    case "pause": player.pauseKey = key; break;
                }
                waitingForBinding = null;
                SaveBindings();
                break;
            }
        }

        private void OnGUI()
        {
            GUI.depth = 0;
            GUIStyle title = new GUIStyle(GUI.skin.label);
            title.fontSize = Mathf.Clamp(Screen.width / 31, 28, 54);
            title.fontStyle = FontStyle.Bold;
            title.normal.textColor = new Color(0.88f, 0.88f, 0.84f);
            title.alignment = TextAnchor.MiddleCenter;
            GUIStyle heading = new GUIStyle(GUI.skin.label);
            heading.fontSize = 17;
            heading.fontStyle = FontStyle.Bold;
            heading.normal.textColor = new Color(0.76f, 0.31f, 0.29f);
            GUIStyle body = new GUIStyle(GUI.skin.label);
            body.fontSize = 14;
            body.wordWrap = true;
            body.normal.textColor = new Color(0.81f, 0.83f, 0.82f);
            GUIStyle small = new GUIStyle(GUI.skin.label);
            small.fontSize = 11;
            small.normal.textColor = new Color(0.56f, 0.61f, 0.63f);
            GUIStyle button = new GUIStyle(GUI.skin.button);
            button.fontSize = 14;
            button.fixedHeight = 34;
            button.normal.textColor = new Color(0.84f, 0.86f, 0.86f);
            button.hover.textColor = Color.white;

            if (mode == UIMode.Title)
            {
                DrawPanel(new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 215f, 480f, 430f));
                GUI.Label(new Rect(Screen.width * 0.5f - 225f, Screen.height * 0.5f - 190f, 450f, 70f), "WHITE NOISE", title);
                GUI.Label(new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 120f, 440f, 28f), "THE LAST WINTER  /  DECEMBER 2004", heading);
                GUI.Label(new Rect(Screen.width * 0.5f - 205f, Screen.height * 0.5f - 65f, 410f, 62f), "A quiet town on the coast of Maine.\nA cabin that remembers more than its owner.", body);
                float y = Screen.height * 0.5f + 16f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 36f), "NEW GAME", button)) StartNewGame();
                y += 46f;
                GUI.enabled = hasSave;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 36f), "CONTINUE", button)) LoadGame();
                GUI.enabled = true;
                y += 46f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 36f), "CONTROLS", button)) mode = UIMode.Settings;
                GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f + 178f, 420f, 20f), "WASD  MOVE     E  INTERACT     C  CAMERA     ESC  MENU", small);
                return;
            }

            if (mode == UIMode.Game)
            {
                GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
                GUI.Box(new Rect(15f, 15f, 280f, 126f), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(28f, 24f, 250f, 22f), "DECEMBER 2004  /  DAY " + day + " OF 6", heading);
                GUI.Label(new Rect(28f, 50f, 250f, 22f), FormatClock() + "     " + currentLocation, body);
                GUI.Label(new Rect(28f, 75f, 250f, 48f), "$" + money + "   FUEL " + fuel + "   FOOD " + food + "\nWOOD " + firewood + "   FISH " + fish + "   MEAT " + venison, small);
                GUI.Label(new Rect(Screen.width - 220f, 18f, 200f, 24f), player.ThirdPerson ? "CAMERA / FOLLOW" : "CAMERA / FIRST PERSON", small);
                if (nearestPoint != null)
                {
                    GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.82f);
                    GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height - 96f, 380f, 56f), GUIContent.none);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(Screen.width * 0.5f - 175f, Screen.height - 88f, 350f, 24f), "[" + player.InteractKey + "]  " + nearestPoint.displayName, body);
                }
                if (messageTimer > 0f)
                {
                    GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.76f);
                    GUI.Box(new Rect(Screen.width * 0.5f - 270f, 26f, 540f, 58f), GUIContent.none);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(Screen.width * 0.5f - 255f, 35f, 510f, 44f), currentMessage, body);
                }
                GUI.Label(new Rect(18f, Screen.height - 28f, 430f, 20f), "WASD MOVE  /  SHIFT WALK FASTER  /  E INTERACT  /  C CAMERA  /  ESC MENU", small);
                return;
            }

            if (mode == UIMode.Pause)
            {
                DrawPanel(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 150f, 380f, 300f));
                GUI.Label(new Rect(Screen.width * 0.5f - 160f, Screen.height * 0.5f - 125f, 320f, 36f), "PAUSED", title);
                float y = Screen.height * 0.5f - 65f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 34f), "RETURN", button)) mode = UIMode.Game;
                y += 43f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 34f), "SAVE", button)) { SaveGame(); SetMessage("Game saved."); mode = UIMode.Game; }
                y += 43f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 34f), "CONTROLS", button)) { pausedBeforeSettings = true; mode = UIMode.Settings; }
                y += 43f;
                if (GUI.Button(new Rect(Screen.width * 0.5f - 130f, y, 260f, 34f), "TITLE SCREEN", button)) { SaveGame(); mode = UIMode.Title; }
                return;
            }

            if (mode == UIMode.Settings)
            {
                DrawPanel(new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f - 255f, 500f, 510f));
                GUI.Label(new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 236f, 440f, 38f), "CONTROLS", title);
                GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - 186f, 420f, 24f), waitingForBinding == null ? "Select a key to rebind." : "PRESS A KEY FOR: " + waitingForBinding.ToUpperInvariant(), heading);
                string[] names = { "Move forward", "Move backward", "Move left", "Move right", "Walk faster", "Interact", "Switch camera", "Pause" };
                string[] keys = { "forward", "back", "left", "right", "sprint", "interact", "camera", "pause" };
                KeyCode[] values = { player.forwardKey, player.backKey, player.leftKey, player.rightKey, player.sprintKey, player.interactKey, player.cameraKey, player.pauseKey };
                for (int i = 0; i < names.Length; i++)
                {
                    float y = Screen.height * 0.5f - 140f + i * 39f;
                    GUI.Label(new Rect(Screen.width * 0.5f - 210f, y, 230f, 30f), names[i], body);
                    string label = waitingForBinding == keys[i] ? "PRESS KEY..." : values[i].ToString();
                    if (GUI.Button(new Rect(Screen.width * 0.5f + 35f, y - 2f, 150f, 30f), label, button)) BeginRebind(keys[i]);
                }
                GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f + 184f, 420f, 24f), "Mouse sensitivity is fixed for this slice. Changes save automatically.", small);
                if (GUI.Button(new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.5f + 215f, 220f, 32f), "BACK", button))
                {
                    waitingForBinding = null;
                    mode = pausedBeforeSettings ? UIMode.Pause : UIMode.Title;
                }
                return;
            }

            if (mode == UIMode.Gear)
            {
                DrawPanel(new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 180f, 460f, 360f));
                GUI.Label(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 160f, 400f, 40f), "MORNING KIT", title);
                GUI.Label(new Rect(Screen.width * 0.5f - 195f, Screen.height * 0.5f - 108f, 390f, 35f), "Choose what you take into the day.", body);
                string[] kits = { "1  FISHING KIT", "2  WORK GLOVES", "3  TRAIL PACK", "4  JOURNAL + LANTERN" };
                for (int i = 0; i < kits.Length; i++)
                {
                    float y = Screen.height * 0.5f - 58f + i * 46f;
                    if (GUI.Button(new Rect(Screen.width * 0.5f - 160f, y, 320f, 36f), kits[i], button)) ChooseGear(i);
                }
                GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f + 132f, 340f, 22f), "ESC TO CLOSE", small);
                return;
            }

            if (mode == UIMode.Dialogue)
            {
                DrawPanel(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f - 205f, 600f, 410f));
                GUI.Label(new Rect(Screen.width * 0.5f - 270f, Screen.height * 0.5f - 182f, 540f, 36f), dialogueTitle, title);
                GUI.Label(new Rect(Screen.width * 0.5f - 270f, Screen.height * 0.5f - 125f, 540f, 130f), dialogueText, body);
                int count = dialogueOptions == null ? 0 : dialogueOptions.Length;
                for (int i = 0; i < count; i++)
                {
                    float y = Screen.height * 0.5f + 16f + i * 48f;
                    if (GUI.Button(new Rect(Screen.width * 0.5f - 245f, y, 490f, 38f), (i + 1) + "  /  " + dialogueOptions[i], button)) ChooseDialogue(i);
                }
                GUI.Label(new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f + 165f, 520f, 20f), "THE TOWN DOES NOT FORGET. IT JUST STOPS TALKING.", small);
                return;
            }

            if (mode == UIMode.Ending)
            {
                DrawPanel(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f - 205f, 600f, 410f));
                GUI.Label(new Rect(Screen.width * 0.5f - 275f, Screen.height * 0.5f - 170f, 550f, 52f), dialogueTitle, title);
                GUI.Label(new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 88f, 520f, 150f), dialogueText, body);
                if (GUI.Button(new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.5f + 108f, 240f, 38f), "RETURN TO TITLE", button)) mode = UIMode.Title;
            }
        }

        private void DrawPanel(Rect rect)
        {
            GUI.color = new Color(0.025f, 0.035f, 0.045f, 0.96f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(0.36f, 0.12f, 0.14f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private string FormatClock()
        {
            int h = Mathf.FloorToInt(clockMinutes / 60f) % 24;
            int m = Mathf.FloorToInt(clockMinutes % 60f);
            string suffix = h >= 12 ? "PM" : "AM";
            int displayHour = h % 12;
            if (displayHour == 0) displayHour = 12;
            return displayHour.ToString("00") + ":" + m.ToString("00") + " " + suffix;
        }
    }
}
