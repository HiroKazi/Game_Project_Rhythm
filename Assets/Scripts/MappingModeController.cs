using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using UnityEditor;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MappingModeController : MonoBehaviour
{

    public Text positionLabel;
    public Text timerLabel;
    public Text playLabel;
    public InputField speedInput;
    public AudioSource audio;

    public GameObject buttonPrefab;
    public float scaleSensitivity;

    private float curTime;
    private float[] curPosition = new float[2];
    private MappingButton curButton;
    private Vector3 lastPosition = new Vector3();

    private Stopwatch mapTimer = new Stopwatch();

    private SortedDictionary<float, ButtonItem> mappedButtons = new SortedDictionary<float, ButtonItem>();
    private SortedList<float, MappingButton> displayingButtons = new SortedList<float, MappingButton>();

    [DllImport("__Internal")]
    private static extern void SaveTextFile(string filename, string text);

    [DllImport("__Internal")]
    private static extern void OpenFile();
    private void Start()
    {
        this.SetupAudioClip();
    }

    private void Awake()
    {
        this.speedInput.text = "1";
    }

    // Update is called once per frame
    private void Update()
    {
        this.UpdatePositionLabel(Input.mousePosition.x, Input.mousePosition.y);
        this.UpdateTimerLabel();

        if (this.mapTimer.IsRunning && Input.GetMouseButtonDown(0))
        {
            this.curPosition[0] = Input.mousePosition.x;
            this.curPosition[1] = Input.mousePosition.y;
            this.curTime = this.mapTimer.ElapsedMilliseconds;

            this.curButton = this.CreateButton(this.curPosition[0], this.curPosition[1]);
            this.lastPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && this.curButton != null)
        {
            Vector3 diffPos = Input.mousePosition - this.lastPosition;

            this.curButton.dragRegion.gameObject.transform.localScale += new Vector3(((Mathf.Abs(diffPos.x) + Mathf.Abs(diffPos.y)) * 1 / 60f * scaleSensitivity), 0f, 0f);
            this.curButton.dragRegion.gameObject.transform.position += ((diffPos) * scaleSensitivity * 0.75f);

            this.curButton.InitializeLastButton(Input.mousePosition.x, Input.mousePosition.y);
            this.lastPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0) && curButton != null)
        {
            ButtonItem button = new ButtonItem();
            button.time = this.curTime + this.GetSpeedInputVal();

            button.position[0] = this.curPosition[0];
            button.position[1] = this.curPosition[1];
            button.isDrag = this.curButton.isDrag;
            button.endPosition[0] = this.lastPosition.x;
            button.endPosition[1] = this.lastPosition.y;

            this.mappedButtons.Add(button.time, button);

            this.curButton = null;
            this.lastPosition = new Vector3();
        }

        if (curButton != null && curButton.buttonTimer.ElapsedMilliseconds > curButton.duration)
        {
            ButtonItem button = new ButtonItem();
            button.time = this.curTime + this.GetSpeedInputVal();

            button.position[0] = this.curPosition[0];
            button.position[1] = this.curPosition[1];
            button.isDrag = this.curButton.isDrag;
            button.endPosition[0] = this.lastPosition.x;
            button.endPosition[1] = this.lastPosition.y;

            this.mappedButtons.Add(button.time, button);

            curButton.DestroyButton();

            this.curButton = null;
            this.lastPosition = new Vector3();
        }
    }

    private void UpdatePositionLabel(float x, float y)
    {
        this.positionLabel.text = "x: " + x + ", y: " + y;
    }

    private MappingButton CreateButton(float x, float y)
    {
        GameObject button = Instantiate(buttonPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        button.transform.SetParent(this.transform, false);

        MappingButton mappingButton = button.GetComponent<MappingButton>();
        mappingButton.duration = this.GetSpeedInputVal();
        mappingButton.InitializeFirstButton(x, y);

        return mappingButton;
    }

    private void ButtonsToHide(float start, float end)
    {
        foreach (KeyValuePair<float, MappingButton> pair in displayingButtons)
        {
            if ((pair.Key - GetSpeedInputVal()) < start)
            {
                Destroy(pair.Value);
            }
            else if (pair.Key + GetSpeedInputVal() > end)
            {
                Destroy(pair.Value);
            }
        }
    }

    private void UpdateTimerLabel()
    {
        this.timerLabel.text = System.TimeSpan.FromMilliseconds(this.mapTimer.ElapsedMilliseconds).ToString();
    }

    public void OnPlayButtonPress()
    {
        if (this.mapTimer.IsRunning)
        {
            if (this.audio.clip != null)
            {
                audio.Pause();
            }

            this.mapTimer.Stop();
            this.playLabel.text = "Start";
        }
        else
        {
            if (audio.clip != null)
            {
                audio.Play();
            }
            if (this.speedInput != null)
            {
                this.speedInput.gameObject.SetActive(false);
            }

            this.mapTimer.Start();
            this.playLabel.text = "Stop";
        }
    }

    private float GetSpeedInputVal()
    {
        float speed = 1f;
        if (this.speedInput != null)
        {
            float.TryParse(this.speedInput.text, out speed);
        }
        return (speed * 1000);
    }

    public void OnMapFinish()
    {
        ButtonData buttonData = new ButtonData();

        foreach (KeyValuePair<float, ButtonItem> pair in this.mappedButtons)
        {
            buttonData.buttons.Add(pair.Value);
        }
        /*#if UNITY_EDITOR
                string filePath_ = EditorUtility.SaveFilePanel("Save localization data file", Application.streamingAssetsPath, "", "json");

                if (!string.IsNullOrEmpty(filePath_))
                {
                    string dataAsJson_ = JsonUtility.ToJson(buttonData);
                    File.WriteAllText(filePath_, dataAsJson_);
                }
        #endif*/
        string dataAsJson = JsonUtility.ToJson(buttonData);

#if UNITY_WEBGL && !UNITY_EDITOR
        SaveTextFile("mapping_data.json", dataAsJson);
#else
        string filePath = Application.persistentDataPath + "/mapping_data.json";
        File.WriteAllText(filePath, dataAsJson);
#endif


    }

    private void SetupAudioClip()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        OpenFile(); // Call JavaScript to open file picker
#else
        string filePath = EditorUtility.OpenFilePanel("Select music file", Application.dataPath + "/Resources", "mp3");
        LoadAudioFromFile(filePath);
#endif
    }
    private void LoadAudioFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        StartCoroutine(LoadAudio(filePath));
    }

    private IEnumerator LoadAudio(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError("Failed to load audio: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                this.audio.clip = clip;
            }
        }

    }
}
