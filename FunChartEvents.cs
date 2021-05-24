using BepInEx;
using BiendeoCHLib;
using BiendeoCHLib.Patches;
using BiendeoCHLib.Patches.Attributes;
using BiendeoCHLib.Wrappers;
using BiendeoCHLib.Wrappers.Attributes;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.GUI;
using Rewired;

/*	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *
 * FunChartEvents Plugin											 *
 * By YoshiOG, with help from the Clone Hero Modding Discord server	 *
 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *
 *    This is "our" source code, I guess?    *
 *		(Use with good faith!)				 *
 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *	 *
 *	 
 */

namespace FunChartEvents
{
	[BepInPlugin("com.yoshiog.funchartevents", "FunChartEvents", "1.0.1")]
	[BepInDependency("com.biendeo.biendeochlib")]
	public class FunChartEvents : BaseUnityPlugin
	{
		// public readonly bool doDebugHUD = false;
		public static FunChartEvents Instance { get; private set; }
		public GameManager gmObj;
		public GameManagerWrapper gameMgr;
		public ScoreManagerWrapper scoreManager;
		private float songSpeed;

		public GameObject myHud;
		public TextMeshProUGUI hudText;
		private string curHudTxt;
		private string oldHudTxt;
		private bool hudWasInit;

		private Camera playerOneCam;
		private float defaultAspect;
		private float highwayWidthFactor;
		private float targetHwWidth;
		private int lastHwwIndex;
		private double timeSinceLastHww;
		private float startHwWidth;
		private bool hwwIsChanging;

		public bool isNotesDotChart;
		private double currentMS;
		private string cmd;
		private string param;
		public uint currentTick;
		public FieldInfo tickField;
		public float cmdTimer;
		public int refreshRate;
		
		public float gemMult;
		public float defGemSize;
		public bool doGemColor;
		public Color gemColor;

		public int timeOfPause;
		public int timeSincePause;
		public bool fretsAreBeingHeld;
		public string fretStr;

		List<ChartCommand> commands;
		private bool hudParsed;
		private string parsedHudTxt;

		private string sceneName;
		private bool sceneChanged;
		private int index;

		private List<string> parameters = new List<string>();
		public FunChartEvents()
		{
			Instance = this;
		}
		#region Unity Methods
		public void Start()
		{
			SceneManager.activeSceneChanged += delegate (Scene _, Scene __)
			{
				sceneChanged = true;
			};
			tickField = typeof(GameManager).GetField("\u030D\u030E\u0313\u0319\u0315\u0314\u0315\u030E\u0315\u0315\u0316");
			GemColorInit();
			isNotesDotChart = false;
			gemMult = 1f;
			defGemSize = 1.1f;
			curHudTxt = "";
			oldHudTxt = "";
			hudWasInit = false;
			hudParsed = false;
			targetHwWidth = 0;
			lastHwwIndex = 0;
			hwwIsChanging = false;
			refreshRate = Screen.currentResolution.refreshRate;
			
			doGemColor = false;
			gemColor = Color.magenta;
		}
		public void LateUpdate()
		{
			sceneName = SceneManager.GetActiveScene().name;
			if (!hudWasInit && sceneName == "Gameplay")
			{
				Transform canvasTransform = FadeBehaviourWrapper.Instance.FadeGraphic.canvas.transform;
				HUDTextInit(canvasTransform);
			}
			if (this.sceneChanged && sceneName == "Gameplay")
			{
				ResetGemColors();

				gmObj = GameObject.Find("Game Manager").GetComponent<GameManager>();
				gameMgr = GameManagerWrapper.Wrap(gmObj);

				cmdTimer = 0;
				refreshRate = Screen.currentResolution.refreshRate;

				songSpeed = gameMgr.GlobalVariables.SongSpeed.GetFloatPercent;

				playerOneCam = gameMgr.BasePlayers[0].Camera;
				defaultAspect = playerOneCam.aspect;
				startHwWidth = defaultAspect;

				gemMult = 1f;
				defGemSize = 1.1f * ((float)gameMgr.GlobalVariables.GameGemSize.CurrentValue / 100);

				lastHwwIndex = 0;
				hwwIsChanging = false;
				isNotesDotChart = gameMgr.GlobalVariables.SongEntry.SongEntry.chartPath.EndsWith("notes.chart");

				if (isNotesDotChart)
				{
					string chartFile = gameMgr.GlobalVariables.SongEntry.SongEntry.chartPath;
					ChartParser parser = new ChartParser(chartFile);

					string[] customEvents = new string[] {	// ==============================|    |
						// // "example",	//  Example event description goes here, explaining
											//	how it works and stuff.
											//	  `example Param1,,Param2[,,Param3]` :
											//		[float] Param1 : Example of a required float
											//		 number parameter, like `3.14`
											//		[string] Param2 : Example of a required string
											//		 parameter.  You don't need to surround the
											//		 string with quotes or anything, BUT it CANNOT
											//		 contain double commas (`,,`)!
											//		 Also, debug variables/tags `{LikeThis}` can 
											//		 be used if you parse the event params using
											//		 `ParseTheText(params)`
											//			 (!) PRO TIP: Because chart events can't
											//				have quote marks (`"`), you can insert
											//				two apostrophes (`''`) in place of
											//				each quote mark, even if not parsed.
											//		[int][?] Param3 : Example of an optional
											//			integer parameter. 
											//			 (?) Defaults to 69 if not given.
											//				Also, the `[?]` here means optional.
						"gemsize",			//	Sets all gems' sizes to a float multiplier.
											//	  `gemsize 1` resets back to normal.
						"hudtext",			//  Makes text appear above the highway.
											//	  `hudtext Message` :
											//		[string] Message : The text to show.
											//			 (!) PRO TIP: Use the various `{Tags}`
											//				in this mod to spice up your text!
						"hudtext_off",		//	Turns off the HUD text above the highway.
						"highwaywidth",		//  Sets the highway width to a multiplier.
											//	Value of 1 resets.

						"gemcolor",			// Sets all gems' colors to a hex value.
											//	  `gemcolor #RRGGBB`
											//		[Color] #RRGGBB : A hex RGB color value.
											//			 (!)
						"gemcolor_off",		//												 |    |
						
					};
					commands = parser.ParseEvents(customEvents);
					index = 0;
					currentMS = -1000;
					timeSinceLastHww = commands[0].Resolution;
				}
				else
				{
					commands = new List<ChartCommand>();
				}
				this.sceneChanged = false;
			}
			if (sceneName == "Gameplay")
			{
				cmdTimer += Time.deltaTime;
				if (cmdTimer >= 1 / (float)refreshRate)
                {
					if (isNotesDotChart)
                    {
						if (index < commands.Count)
						{
							currentTick = GetCurrentTick(gmObj);
							if (currentTick == 0)
							{
								currentMS = TimeSpan.FromSeconds(gameMgr.SongTime).TotalMilliseconds * songSpeed;
							}
							else
							{
								currentMS = 0;
							}
							if (currentMS < -900)
							{
								// gmObj = GameObject.Find("Game Manager").GetComponent<GameManager>();
								gameMgr = GameManagerWrapper.Wrap(gmObj);
								curHudTxt = "";
								oldHudTxt = "";
							}
							if (ReachedTick(commands[index].Tick))
							{
								ChartCommand curCommand = commands[index++];
								cmd = curCommand.Command;
								param = curCommand.Parameter;
								parameters = param.Split(paramSeparators, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
								if (cmd == "gemsize")
								{
									if (float.TryParse(param, out gemMult))
									{
										foreach (var gameObj in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
										{
											if (gameObj.name.StartsWith("Note"))
											{
												gameObj.transform.localScale = new Vector3(defGemSize * gemMult, defGemSize * gemMult, 1f);
											}
										}
									}
								}
								if (cmd == "hudtext")
								{
									curHudTxt = param;
									hudParsed = false;
									if (oldHudTxt != curHudTxt)
									{
										oldHudTxt = curHudTxt;
									}
									hudText.enabled = true;
									parsedHudTxt = this.ParseTags(param, out hudParsed);
									SetHUDText(parsedHudTxt);
								}
								if (cmd == "hudtext_off")
								{
									hudParsed = false;
									if (oldHudTxt != curHudTxt)
									{
										oldHudTxt = curHudTxt;
									}
									hudText.enabled = false;
								}
								if (cmd == "highwaywidth")
								{
									if (float.TryParse(param, out targetHwWidth))
									{
										if (targetHwWidth != 0)
										{
											lastHwwIndex = index - 1;
											startHwWidth = defaultAspect / playerOneCam.aspect;
											timeSinceLastHww -= commands[0].Resolution;
											hwwIsChanging = true;
										}
									}
								}
								if (cmd == "gemcolor")
								{
									doGemColor = true;
									if (ColorUtility.TryParseHtmlString(param, out gemColor))
									{
										SetGemColors(gemColor, gemColor, gemColor, gemColor, gemColor);
									}
								}
								if (cmd == "gemcolor_off")
								{
									ResetGemColors();
								}
							}
						}
						if (commands.Count > 0)
                        {
							if (gameMgr.IsPaused)
							{
								hudText.enabled = false;
							}
							if (!gameMgr.IsPaused)
							{
								parsedHudTxt = this.ParseTags(curHudTxt, out hudParsed);
								if (hudParsed)
								{
									SetHUDText(parsedHudTxt);
								}
							}
							if (hwwIsChanging)
							{
								timeSinceLastHww = currentTick - commands[lastHwwIndex].Tick;
								highwayWidthFactor = (float)timeSinceLastHww / commands[0].Resolution * (targetHwWidth - startHwWidth) + startHwWidth;
								if (timeSinceLastHww < commands[0].Resolution && highwayWidthFactor != 0)
								{
									if (!gameMgr.IsPaused)
									{
										playerOneCam.aspect = defaultAspect / highwayWidthFactor;
									}
									else
									{
										playerOneCam.aspect = defaultAspect / targetHwWidth;
										hwwIsChanging = false;
									}
								}
								if (timeSinceLastHww > commands[0].Resolution+3 && playerOneCam.aspect == defaultAspect / targetHwWidth)
								{
									hwwIsChanging = false;
								}
								if (timeSinceLastHww >= commands[0].Resolution || gameMgr.IsPaused)
								{
									if (highwayWidthFactor != targetHwWidth)
                                    {
										playerOneCam.aspect = defaultAspect / targetHwWidth;
									}
								}
							}
						}
					}
					cmdTimer -= 1 / (float)refreshRate;
				}
			}
		}
		#endregion
		public uint GetCurrentTick(GameManager gm)
		{  // Get current chart tick position in song.  Requires a defined GameManager object.
			return (uint)(tickField.GetValue(gm));
		}
		public bool ReachedTick(uint position, GameManager gm = null)
		{  // Whether or not a tick position was reached in the current song
			if (gm == null && gmObj != null)
            {
				gm = gmObj;
			}
			if (gm == null)
            {
				return false;
            }
			return GetCurrentTick(gm) >= position;
		}
		private void SetHUDText(string text)
		{  // To be used with `hudtext` event.
			if (hudWasInit)
			{
				hudText.enabled = true;
				hudText.text = text;
				hudText.ForceMeshUpdate();
			}
		}

		/*
		 * Convert string containing any chars in "GRYBO" or "1234546" to boolean array of which chars it contained.
		 * Supports 5-fret and 6-fret (with GRYBO equivalent to buttons 1 to 5 respectively).
		 * Also case-insensitive, so `grybo` returns same as `GRYBO` or `12345`.
		 *	[!] Keep in mind that the value indexes are button # minus 1, so
		 *		Orange/White2/Button5 is `FretsHeld.ParseGRYBO(frets)[4]`
		 * 
		 * Examples:
		 *		||	String			   |||	First 6 bool values in returned array  |||	Comments
		 *		|	"five", "LEL", "h"	|	False,False,False,False,False,False		|	No frets, no value
		 *		|	"GRYBO"				|	TRUE, TRUE, TRUE, TRUE, TRUE, False		|	Green+Red+Yellow+Blue+Orange
		 *		|	"123"				|	TRUE, TRUE, TRUE, False,False,False		|	All Black GHL frets
		 *		|	"456"				|	False,False,False,TRUE, TRUE, TRUE		|	All White GHL frets
		 *		|	"g"					|	TRUE, False,False,False,False,False		|	(sorry for the following, haha)
		 *		|	"r"					|	False,TRUE, False,False,False,False		|					 W
		 *		|	"y"					|	False,False,TRUE, False,False,False		|			 E
		 *		|	"YBO"				|	False,False,TRUE, TRUE, TRUE, False		|	 B
		 */
		public class HeldFrets
		{
			private static bool[] DoGRYBO(string fretties, bool[] bools, ref bool anyPressed)
			{   // Five Nights at Fretty's

				bools[0] = (fretties.ToLower().Contains("g") || fretties.Contains("1"));

				bools[1] = (fretties.ToLower().Contains("r") || fretties.Contains("2"));

				bools[2] = (fretties.ToLower().Contains("y") || fretties.Contains("3"));

				bools[3] = (fretties.ToLower().Contains("b") || fretties.Contains("4"));

				bools[4] = (fretties.ToLower().Contains("o") || fretties.Contains("5"));

				bools[5] = fretties.Contains("6");

				anyPressed = (bools != noFrets);
				return bools;
			}
			public static bool[] ParseGRYBO(string grybo6)
			{
				bool[] fretBools = noFrets;
				bool _ = false;
				DoGRYBO(grybo6, fretBools, ref _);
				return fretBools;
			}
			public static bool[] ParseGRYBO(string grybo6, out bool detectedPress)
			{
				bool[] fretBools = noFrets;
				detectedPress = false;
				DoGRYBO(grybo6, fretBools, ref detectedPress);
				return fretBools;
			}
			private static bool[] JustParse(byte byteOf87, out bool detectedPress)
			{
				bool[] fretBools = ConvertByteToBoolArray(byteOf87);
				detectedPress = false;
				return fretBools;
			}
			public static bool[] ParseByte(byte wasThatThe)
			{
				return JustParse(wasThatThe, out bool ofEightySeven);
			}
			public static bool[] ParseByte(byte wasThatTheByte, out bool ofEightySeven)
			{
				return JustParse(wasThatTheByte, out ofEightySeven);
			}
		}       // ... Was that the Bite of '87?!
		public static bool[] noFrets =
		{
			false, false, false, false, false, false, false, false
		};
		public static string GetGRYBO(bool[] fretArray, bool plainText = true, bool isGHL = false)
		{
			string retGrybo = "";
			if (!isGHL && !fretArray[5])
			{
				if (fretArray[0])
				{
					if (plainText) { retGrybo += "G"; }
					else { retGrybo += "<color=#00CC00FF>G</color>"; }
				}
				else
				{
					if (!plainText) { retGrybo += "<color=#00000000>G</color>"; }
				}
				if (fretArray[1])
				{
					if (plainText) { retGrybo += "R"; }
					else { retGrybo += "<color=#FF3333FF>R</color>"; }
				}
				else
				{
					if (!plainText) { retGrybo += "<color=#00000000>R</color>"; }
				}
				if (fretArray[2])
				{
					if (plainText) { retGrybo += "Y"; }
					else { retGrybo += "<color=#CCCC00FF>Y</color>"; }
				}
				else
				{
					if (!plainText) { retGrybo += "<color=#00000000>Y</color>"; }
				}
				if (fretArray[3])
				{
					if (plainText) { retGrybo += "B"; }
					else { retGrybo += "<color=#9999FFFF>B</color>"; }
				}
				else
				{
					if (!plainText) { retGrybo += "<color=#00000000>B</color>"; }
				}
				if (fretArray[4])
				{
					if (plainText) { retGrybo += "O"; }
					else { retGrybo += "<color=#EE7700FF>O</color>"; }
				}
				else
				{
					if (!plainText) { retGrybo += "<color=#00000000>O</color>"; }
				}
			}
			else
			{
				// TO DO: make text strings for 6 fret
			}
			return retGrybo;
		}
		private readonly string[] tagVars = new string[] {  // Debug tag things that can be used with `hudtext`.
			"{HeldButtons}",		// [string]		Colored text showing Player 1's held frets.
									//					Returns "GRYBO" but letters of frets not held
									//					are turned invisible.
			"{Player}",				// [string]		Name of Player 1.  Pretty self-explanatory.
			"{Combo}",				// [int]		Current note streak of Player 1.
			"{FC}", "{/FC}",		// [bool]		Text between these tags is only shown if Player 1
									//					is on an FC run AND not a bot.
			"{-FC}", "{/-FC}",		// [bool]		Text between these tags is only shown if Player 1
									//					IS NOT on an FC run, or if it's a bot.
			"{FretsToBits}",		// [byte]	    Returns binary bits of current frets. Each bit is
									//					colored cyan if 1, white if 0.
			"{SongTick}",			// [int]		Current song time in chart ticks.
		};                          // (!!!) These tag names are case-sensitive!
		public string ParseTags(string input)  // ONLY USE `ParseTags` for parsing the tags above, NOT `ParseTextTags`!!
		{
			string ree = ParseTextTags(input, out bool _);
			return ree;
		}
		public string ParseTags(string input, out bool boolOut)
		{
			string ree = ParseTextTags(input, out boolOut);
			return ree;
		}
		public string ParseTextTags(string text, out bool outputBool)
		{
			bool didFindVars = false;
			string ret = text.Replace("''","\"");

			bool isBot = gameMgr.BasePlayers[0].Player.PlayerProfile.Bot.GetBoolValue;
			bool isFC = gameMgr.BasePlayers[0].FCIndicator.activeSelf;
			string repl = "";
			foreach (string debugVar in tagVars)
			{
				if (ret.Contains(debugVar))
				{
					if (debugVar == "{SongTick}")
					{
						repl = GetCurrentTick(gmObj).ToString();
					}
					if (debugVar == "{FretsToBits}")
					{
						bool[] curFrets = ConvertByteToBoolArray(gameMgr.BasePlayers[0].FretsHeld);
						foreach (bool bit in curFrets)
						{
							if (bit) { repl += "<color=#00FFFFFF>1</color>"; }
							else { repl += "0"; }
						}
					}
					if (debugVar == "{HeldButtons}")
					{
						bool[] curFrets = ConvertByteToBoolArray(gameMgr.BasePlayers[0].FretsHeld);
						repl = GetGRYBO(curFrets, false);
					}
					if (debugVar == "{Player}")
					{
						repl = gameMgr.BasePlayers[0].Player.PlayerProfile.PlayerName;
					}
					if (debugVar == "{Combo}")
					{
						repl = gameMgr.BasePlayers[0].Combo.ToString();
					}
					if (isFC && !isBot)
					{
						if (debugVar == "{FC}")
						{
							repl = "<size=100%>";
						}
						if (debugVar == "{-FC}")
						{
							repl = "<size=0%>";
						}
					}
					if (!isFC && !isBot)
					{
						if (debugVar == "{FC}")
						{
							repl = "<size=0%>";
						}
						if (debugVar == "{-FC}")
						{
							repl = "<size=100%>";
						}
					}
					if (isBot)
                    {
						if (debugVar == "{FC}")
						{
							repl = "<size=0%>";
						}
						if (debugVar == "{-FC}")
						{
							repl = "<size=0%>";
						}
					}
					if (debugVar == "{/FC}" || debugVar == "{/-FC}")
					{ 
						repl = "</size>";
					}
					if (repl != "")
					{
						didFindVars = true;
						// dbgParsedTxt = repl;
						ret = ret.Replace(debugVar, repl);
						repl = "";
					}
				}
			}
			outputBool = didFindVars;
			return ret;
		}
		public string[] paramSeparators =
		{
			",,"
		};
		private void HUDTextInit(Transform trans)
		{
			if (!hudWasInit)
			{
				myHud = new GameObject("CustomHUD");
				myHud.layer = LayerMask.NameToLayer("UI");
				myHud.SetActive(true);
				myHud.transform.SetParent(trans);
				myHud.transform.localPosition = new Vector3();
				myHud.transform.localEulerAngles = new Vector3();
				myHud.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f); // */
				Font fontLato = (Font)Resources.GetBuiltinResource(typeof(Font), "Lato-Bold.ttf");
				hudText = myHud.AddComponent<TextMeshProUGUI>();
				hudText.enabled = true;
				hudText.SetClipRect(new Rect(-Screen.width / 2, -Screen.height / 2, Screen.width, Screen.height), true);
				hudText.rectTransform.sizeDelta = new Vector2(Screen.height, Screen.height / 20);
				hudText.fontSize = 32;
				hudText.font = TMP_FontAsset.CreateFontAsset(fontLato);
				hudText.fontStyle = FontStyles.Bold;
				hudText.alignment = TextAlignmentOptions.Center;
				hudText.enableAutoSizing = true;
				hudText.enableWordWrapping = false;
				hudText.characterSpacing = -5f;
				hudText.color = Color.white;
				hudText.transform.localPosition = new Vector3(0f, Screen.height / 12, 0f);
				hudText.text = "<size=0>sus</color>";
				hudText.ForceMeshUpdate();

				hudWasInit = true;
			}
		}
		private readonly Assembly asm = Assembly.Load("Assembly-CSharp");
		public readonly Color[] defaultGemColors = new Color[]
		{
			new Color(0f, 1f, 0f),
			new Color(1f, 0f, 0f),
			new Color(1f, 1f, 0f),
			new Color(0f, 0.541f, 1f),
			new Color(1f, 0.702f, 0f),
			new Color(0.733f, 0f, 1f)
		};
		public readonly Color[] defaultAnimColors = new Color[]
		{
			new Color(0f, 1f, 0f),
			new Color(1f, 0.549f, 0.549f),
			new Color(1f, 1f, 0.345f),
			new Color(0.47f, 0.823f, 1f),
			new Color(1f, 0.749f, 0.16f)
		};
		public readonly Color[] defaultFretColors = new Color[]
		{
			new Color(0f, 1f, 0f),
			new Color(1f, 0f, 0f),
			new Color(1f, 1f, 0f),
			new Color(0f, 0.776f, 1f),
			new Color(1f, 0.827f, 0.235f)
		};
		public FieldInfo noteColorFI;
		public FieldInfo noteAnimFI;
		public FieldInfo susColorFI;  // When the imposter is sustain
		private void GemColorInit()
        {
			byte[] noteFieldBytes = Encoding.Unicode.GetBytes("\u0319\u0317\u0318\u031a\u030e\u0314\u030d\u0313\u0315\u0311\u031c");
			Type noteFieldType = typeof(byte); // placeholder type
			foreach (Type type in asm.GetTypes())
			{
				if (type.Namespace == null && BitConverter.ToString(Encoding.Unicode.GetBytes(type.Name)) == BitConverter.ToString(noteFieldBytes))
				{
					noteFieldType = type;
				}
			}
			noteColorFI = noteFieldType.GetField("\u030e\u031a\u031a\u0313\u0317\u0314\u0311\u0316\u031a\u0316\u0314");
			noteAnimFI = noteFieldType.GetField("\u0314\u0310\u031C\u0315\u031A\u0311\u0318\u030F\u0315\u0313\u031B");
			susColorFI = noteFieldType.GetField("\u030E\u031C\u031B\u0319\u031B\u0315\u0318\u031B\u0319\u0312\u031C");
		}
		public void SetGemColors(Color g, Color r, Color y, Color b, Color o)
        {
            Color[] newGemColors = new Color[] { g, r, y, b, o, defaultGemColors[5] };
			noteColorFI.SetValue(null, newGemColors);
			SetGemAnim(g, r, y, b, o);
			SetSustainColors(g, r, y, b, o);
		}
		public void SetGemAnim(Color g, Color r, Color y, Color b, Color o)
        {
			Color[] newAnimColors = new Color[] { g, r, y, b, o };
			noteAnimFI.SetValue(null, newAnimColors);
        }
		public void SetSustainColors(Color g, Color r, Color y, Color b, Color o)
        {
			Color[] newFretColors = new Color[] { g, r, y, b, o };
			susColorFI.SetValue(null, newFretColors);
        }
		public void ResetGemColors()
        {
			SetGemColors(defaultGemColors[0], defaultGemColors[1], defaultGemColors[2], defaultGemColors[3], defaultGemColors[4]);
			SetGemAnim(defaultAnimColors[0], defaultAnimColors[1], defaultAnimColors[2], defaultAnimColors[3], defaultAnimColors[4]);
			SetSustainColors(defaultFretColors[0], defaultFretColors[1], defaultFretColors[2], defaultFretColors[3], defaultFretColors[4]);
		}
		private static bool[] ConvertByteToBoolArray(byte b)
		{
			// prepare the return result
			bool[] result = new bool[8];

			// check each bit in the byte. if 1 set to true, if 0 set to false
			for (int i = 0; i < 8; i++)
				result[i] = (b & (1 << i)) != 0;

			// reverse the array (don't)
			// Array.Reverse(result);

			return result;
		}
	}
}