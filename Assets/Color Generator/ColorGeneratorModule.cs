using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

public class ColorGeneratorModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable Red;
    public KMSelectable Green;
    public KMSelectable Blue;
    public KMSelectable Multiply;
    public KMSelectable Reset;
    public KMSelectable Submit;
    Material[] Materials; // Red, Green, Blue, Submit, Multiply
    private static Color[] DefaultColors = new Color[] { RGBColor(237, 28, 36), RGBColor(34, 177, 76), RGBColor(63, 72, 204) };

    int[] serialNumbers = new int[] { 0, 0, 0, 0, 0, 0 };

    int multiplier = 1;
    int red = 0;
    int green = 0;
    int blue = 0;
    int desiredred = 0;
    int desiredgreen = 0;
    int desiredblue = 0;
    bool solved = false;

    static int idCounter = 1;
    int moduleID;

    private static Color RGBColor(int r, int g, int b)
    {
        return new Color((float) r / 255, (float) g / 255, (float) b / 255);
    }

    protected void Start()
    {
        moduleID = idCounter++;

        BombModule.OnActivate += getAnswer;
        Red.OnInteract += HandlePressRed;
        Green.OnInteract += HandlePressGreen;
        Blue.OnInteract += HandlePressBlue;
        Multiply.OnInteract += HandlePressMultiply;
        Reset.OnInteract += HandlePressReset;
        Submit.OnInteract += HandlePressSubmit;

        Materials = new KMSelectable[] { Red, Green, Blue, Multiply, Reset, Submit }.Select(selectable => selectable.GetComponent<Renderer>().material).ToArray();

        int index = 0;
        foreach (Material mat in Materials)
        {
            mat.color = DefaultColors[index % 3];
            index++;
        }
    }

    public void Log(params object[] args)
    {
        Log(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
    }

    public void Log(string format, params object[] args)
    {
        Debug.LogFormat(string.Format("[Color Generator #{0}] {1}", moduleID, format), args);
    }

    void getAnswer()
    {
        string serial = "AB1CD2";

        List<string> data = BombInfo.QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);

        foreach (string response in data)
        {
            Dictionary<string, string> responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            serial = responseDict["serial"];
            break;
        }

        serialNumbers = serial.Select(c =>
        {
            int n;
            if (char.IsLetter(c))
            {
                n = c - 'A' + 1;
            }
            else if (char.IsDigit(c))
            {
                n = c - '0';
            }
            else
            {
                BombModule.HandlePass();
                throw new NotSupportedException("The serial number contains something that's not a letter or a number.");
            }

            return n % 16;
        }).ToArray();

        desiredred = (serialNumbers[0] * 16) + (serialNumbers[1] * 1);
        desiredgreen = (serialNumbers[2] * 16) + (serialNumbers[3] * 1);
        desiredblue = (serialNumbers[4] * 16) + (serialNumbers[5] * 1);

        Log("Your color code is {0} {1} {2} (#{3})", desiredred, desiredgreen, desiredblue, string.Join("", serialNumbers.Select(x => x.ToString("X")).ToArray()));
    }

    private void HandleButtonPress()
    {
        KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
    }

    bool HandlePressRed()
    {
        HandleButtonPress();

        red += multiplier;

        return false;
    }

    bool HandlePressGreen()
    {
        HandleButtonPress();

        green += multiplier;

        return false;
    }

    bool HandlePressBlue()
    {
        HandleButtonPress();

        blue += multiplier;

        return false;
    }

    IEnumerator ShowFinalColor()
    {
        Color finalColor = RGBColor(desiredred, desiredgreen, desiredblue);
        for (int i = 0; i <= 100; i++)
        {
            for (int index = 0; index < 3; index++)
            {
                Materials[index].color = Color.Lerp(DefaultColors[index], finalColor, (float) i / 100);
            }

            yield return new WaitForSeconds(0.01f);
        }
    }

    bool HandlePressSubmit()
    {
        if (solved) return false;

        HandleButtonPress();

        if (red == desiredred && green == desiredgreen && blue == desiredblue)
        {
            BombModule.HandlePass();
            Log("Submitted the correct color! Module solved.");
            solved = true;

            StartCoroutine(ShowFinalColor());
        }
        else
        {
            BombModule.HandleStrike();
            Log("Submitted an incorrect color! ({0}, {1}, {2}) Module reset.", red, green, blue);

            red = 0;
            green = 0;
            blue = 0;
            multiplier = 1;
        }

        return false;
    }

    bool HandlePressReset()
    {
        HandleButtonPress();

        red = 0;
        green = 0;
        blue = 0;
        multiplier = 1;

        return false;
    }

    bool HandlePressMultiply()
    {
        HandleButtonPress();

        multiplier *= 10;
        if (multiplier > 100)
        {
            multiplier = 1;
        }

        return false;
    }

    public string TwitchHelpMessage = "Submit a color using !{0} submit 123 123 123.";

    public IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 4 && split[0] == "submit")
        {
            int red;
            int green;
            int blue;

            if (int.TryParse(split[1], out red) && int.TryParse(split[2], out green) && int.TryParse(split[3], out blue))
            {
                yield return null;

                Reset.OnInteract();
                yield return new WaitForSeconds(0.1f);

                KMSelectable[] buttons = new KMSelectable[] { Red, Green, Blue };
                int[] values = new int[] { red, green, blue };
                for (int i = 0; i < 3; i++)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        for (int x = 0; x < values[index] % 10; x++)
                        {
                            buttons[index].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }

                        values[index] /= 10;
                    }

                    Multiply.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }

                Submit.OnInteract();
            }
        }
    }
}
