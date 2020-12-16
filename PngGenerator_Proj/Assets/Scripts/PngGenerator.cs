using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class PngGenerator : MonoBehaviour
{
    #pragma warning disable 0649
        // [SerializeField] string input;
        // [SerializeField] string output;
        // [SerializeField] int minSize;
        // [SerializeField] int pixelSize;
        [SerializeField] InputField inputField;
        [SerializeField] InputField outputField;
        [SerializeField] InputField pixelsField;
        [SerializeField] InputField boxField;
        [SerializeField] Button button;
        [SerializeField] Text btnText;
        [SerializeField] Text logText;
    #pragma warning restore 0649
        private string logStr;
        private string input;
        private string output;
        private int minPixels;
        private int minBox;
    public class ZoneInfo 
    {
        public int zoneId;
        public Color32 sketchColor;
        public int sketchId;
        public int size;
        public int x;
        public int y;
        public List<int> pixels = new List<int>();
    }
    public class PixelsInfo 
    {
        public int pixelsId;
        public Color32 clr;
        public List<int> pixels = new List<int>();
        public int x;
        public int y;
        public int row;
        public int col;
    }
    public class SketchJsonData
    {
        public int id;
        public int r;
        public int g;
        public int b;
        public List<int> zones = new List<int>();
    }
    public class ZoneJsonData
    {
        public int uid;
        public int centerX;
        public int centerY;
        public int diameter;
        public int sketchId;
    }
    public class PngJsonData
    {
        public List<SketchJsonData> sketches = new List<SketchJsonData>();
        public List<ZoneJsonData> zones = new List<ZoneJsonData>();
    }
    private List<PixelsInfo> pixelsList = new List<PixelsInfo>();
    private List<ZoneInfo> zoneList = new List<ZoneInfo>();
    private List<Color32> sketchList = new List<Color32>();
    private int[] queryList;
    private int zoneCount;
    private int pixelsIdCount;
    private int width = 1536;
    private int height = 1536;
    void Start()
    {
       pixelsField.text = "10";
       boxField.text = "2";
       inputField.text = "/Users/umiringo/Desktop/in";
       outputField.text = "/Users/umiringo/Desktop/out";
       EnableBtn();
    }
    public void Generate()
    {
        DisableBtn();
        input = inputField.text;
        output = outputField.text;
        minPixels = Int32.Parse(pixelsField.text);
        minBox = Int32.Parse(boxField.text);
        StartCoroutine(GenerateCoroutine());

    }
    IEnumerator GenerateCoroutine()
    {
        logStr = string.Empty;
        logText.text = logStr;
        yield return null;
        List<string> pathList = new List<string>();
        var dir = new DirectoryInfo(input);
        string fileName = string.Empty;
        if (dir.Exists) {
            foreach (FileInfo info in dir.GetFiles("*.png")) {
                fileName = info.FullName.ToString();
                pathList.Add(fileName);
            }
        } else {
            ShowErrorLog("找不到目录，请检查!");
            EnableBtn();
            yield break;
        }
        if(pathList.Count == 0) {
            ShowErrorLog("目录下没有符合条件的文件，请检查！");
            EnableBtn();
            yield break;
        }
        ShowInfoLog("检查开始，目录" + input + "下共有" + pathList.Count + "个文件..." + ", 最小像素限制：" + minPixels + ", 最小内切长度限制：" + minBox);
        foreach(var p in pathList) {
            yield return null;
            var level = Path.GetFileNameWithoutExtension(p);
            ShowInfoLog("开始处理" + level + "...");
            var ret = GenerateResourceNew(level, p);
            if(ret != String.Empty) {
                ShowErrorLog(level + "数据错误：" + ret);
                yield return null;
                continue;
            }
        }
        ShowInfoLog("检查结束");
        EnableBtn();
    }

    private void DisableBtn()
    {
        button.interactable = false;
        btnText.text = "检查中...";
    }
    private void EnableBtn()
    {
        button.interactable = true;
        btnText.text = "开始检查";
    }
    private void ShowInfoLog(string info)
    {
        logStr += info + "\n";
        logText.text = logStr;
    }
    private void ShowErrorLog(string error)
    {
        logStr += "<color=#F83434>" + error + "\n</color>";
        logText.text = logStr;
    }
    public void ReduceColor(string level, string linePath)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // 初始化
        zoneCount = 1;
        pixelsIdCount = 1;
        int smallPixels = 0;
        pixelsList.Clear();
        zoneList.Clear();
        sketchList.Clear();
        queryList = new int[width * height];
        // 先讀取文件
        Texture2D lineTex = LoadTexture(linePath);
        if(lineTex == null) return;
        // 圖片生成為rawdata
        byte[] lineRaw = lineTex.GetRawTextureData();
        UnityEngine.Debug.Log(level + " Scan Start... line size = " + lineRaw.Length);
        // for(int x = 0; x < width; ++x) {
        //     for(int y = 0; y < height; ++y) {
        //         int i = y*width + x;
        //         if(GetAlpha(lineRaw, i*4) < 255) continue; 
        //         if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
        //         FloodFillNew(x, y, lineRaw, ref smallPixels);
        //     }
        // }
        UnityEngine.Debug.Log("smallPixels = " + smallPixels);
       Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.LoadRawTextureData(lineRaw);
        byte[] savePngBytes = texture.EncodeToPNG();
        SavePng(savePngBytes, output + "/" + level, level + "_zone.png");
    }
    public byte ColorSplit(byte r)
    {
        // if(r < 16) return 0;
        // else if(r >= 16 && r < 56) return 16;
        // else if(r >= 56 && r < 96) return 56;
        // else if(r >= 96 && r < 136) return 96;
        // else if(r >= 136 && r < 176) return 136;
        // else if(r >= 176 && r < 216) return 176;
        // else return 216;
        var levs = Mathf.Ceil(Mathf.Pow(300, 1.0f/3.0f));
        return (byte)Mathf.RoundToInt(Mathf.Round(((float)r / 255.0f) * levs) / levs * 255.0f);
    }
    public string GenerateResourceNew(string level, string linePath)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // 初始化
        zoneCount = 1;
        pixelsIdCount = 1;
        pixelsList.Clear();
        zoneList.Clear();
        sketchList.Clear();
        queryList = new int[width * height];
        int sketches = 0;
        int zones = 0;
        int smallPixels = 0;
        int smallBox = 0;
        // 先讀取文件
        Texture2D lineTex = LoadTexture(linePath);
        if(lineTex == null) return "图片加载失败，路径为：" + linePath;
        // 圖片生成為rawdata
        byte[] lineRaw = lineTex.GetRawTextureData();
        int hash = Mathf.Abs(GetStableHashCode(level)) % 4;
        UnityEngine.Debug.Log(level + " Scan Start... line size = " + lineRaw.Length + ", hash = " + hash);
        for(int x = 0; x < width; ++x) {
            for(int y = 0; y < height; ++y) {
                int i = y*width + x;
                if(GetAlpha(lineRaw, i*4) < 255) {
                    lineRaw[i*4 + 1] = 0;
                    lineRaw[i*4 + 2] = 0;
                    lineRaw[i*4 + 3] = 0;
                }
            }
        }
        if(hash == 0) {
            for(int x = 0; x < width; ++x) {
                for(int y = 0; y < height; ++y) {
                    int i = y*width + x;
                    if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                    if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                    FloodFill(x, y, lineRaw, ref smallPixels);
                }
            }
        } else if(hash == 1) {
            for(int x = width - 1; x >= 0; --x) {
                for(int y = 0; y < height; ++y) {
                    int i = y*width + x;
                    if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                    if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                    FloodFill(x, y, lineRaw, ref smallPixels);
                }
            }
        } else if(hash == 2) {
            for(int x = 0; x < width; ++x) {
                for(int y = height - 1; y >= 0; --y) {
                    int i = y*width + x;
                    if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                    if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                    FloodFill(x, y, lineRaw, ref smallPixels);
                }
            }
        } else if(hash == 3) {
            for(int x = width - 1; x >= 0; --x) {
                for(int y = height - 1; y >= 0; --y) {
                    int i = y*width + x;
                    if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                    if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                    FloodFill(x, y, lineRaw, ref smallPixels);
                }
            }
        }
        // 對已經記錄的表進行掃描，寻找最小内切正方形, 確定坐標和size
        GenerateInnerBox(lineRaw, ref smallBox);
        //根據結果生成json文件
        GenerateJson(level, ref sketches, ref zones);
        //根據結果從新計算顔色生成假圖
        GenerateFakeNew(level, lineRaw);
        sw.Stop();
        ShowInfoLog("<color=#03C5FF>" + level + "检查完成。</color>颜色种类：" + sketches + ", 色块数量: " + zones + "。 <color=#F3F23A>像素极少区域数：" + smallPixels + ", 内切过小区域数：" + smallBox + "</color>"  + ", 耗时：" + sw.ElapsedMilliseconds + "毫秒");
        return string.Empty;
    }
    private void FloodFillNew(int x, int y, byte[] lineRaw)
    {
        int offset = width * y + x;
        byte r = lineRaw[4 * offset + 1];
        byte g = lineRaw[4 * offset + 2];
        byte b = lineRaw[4 * offset + 3];
        Queue<int> queue = new Queue<int>();
        Queue<int> queue2 = new Queue<int>();
        List<int> tmpPixels = new List<int>();
        queue.Enqueue(x);
        queue2.Enqueue(y);
        int count = 0;
        int mark = 100;
        while (queue.Count > 0) {
            int num = queue.Dequeue();
            int num2 = queue2.Dequeue();
            if (num2 - 1 > -1) {
                int num3 = width* (num2 - 1) + num;
                int num4 = num3 * 4;
                if( ((CheckColor(lineRaw, num4, r, g, b) && GetAlpha(lineRaw, num4) == 255)) || CheckColor(lineRaw, num4, 0, 0, 0) ) {
                    if(!CheckColor(lineRaw, num4, 0, 0, 0)) {
                        queue.Enqueue(num);
                        queue2.Enqueue(num2 - 1);
                    }
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4 + 1] = r;
                    lineRaw[num4 + 2] = g;
                    lineRaw[num4 + 3] = b;
                    lineRaw[num4] = (byte)mark;
                }
            }
            if (num + 1 < width) {
                int num3 = width * num2 + (num + 1);
                int num4 = num3 * 4;
                if( ((CheckColor(lineRaw, num4, r, g, b) && GetAlpha(lineRaw, num4) == 255)) || CheckColor(lineRaw, num4, 0, 0, 0) ) {
                    if(!CheckColor(lineRaw, num4, 0, 0, 0)) {
                        queue.Enqueue(num+1);
                        queue2.Enqueue(num2);
                    }
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4 + 1] = r;
                    lineRaw[num4 + 2] = g;
                    lineRaw[num4 + 3] = b;
                    lineRaw[num4] = (byte)mark;
                }
            }
            if (num - 1 > -1) {
                int num3 = width * num2 + (num - 1);
                int num4 = num3 * 4;
                if( ((CheckColor(lineRaw, num4, r, g, b) && GetAlpha(lineRaw, num4) == 255)) || CheckColor(lineRaw, num4, 0, 0, 0) ) {
                    if(!CheckColor(lineRaw, num4, 0, 0, 0)) {
                        queue.Enqueue(num-1);
                        queue2.Enqueue(num2);
                    }
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4 + 1] = r;
                    lineRaw[num4 + 2] = g;
                    lineRaw[num4 + 3] = b;
                    lineRaw[num4] = (byte)mark;
                }
            }
        }
    }
    private void FloodFill(int x, int y, byte[] lineRaw, ref int smallPixels)
    {
        int offset = width * y + x;
        byte r = lineRaw[4 * offset + 1];
        byte g = lineRaw[4 * offset + 2];
        byte b = lineRaw[4 * offset + 3];
        Queue<int> queue = new Queue<int>();
        Queue<int> queue2 = new Queue<int>();
        List<int> tmpPixels = new List<int>();
        queue.Enqueue(x);
        queue2.Enqueue(y);
        int count = 0;
        int mark = 100;
        int maxX = 0;
        int minX = int.MaxValue;
        int maxY = 0;
        int minY = int.MaxValue;
        if( CheckColor(lineRaw, offset*4, r, g, b) && GetAlpha(lineRaw, offset*4) == 255) {
            ++count;
            tmpPixels.Add(offset);
            lineRaw[offset*4] = (byte)mark;
        }
        while (queue.Count > 0) {
            int num = queue.Dequeue();
            int num2 = queue2.Dequeue();
            if (num2 - 1 > -1) {
                int num3 = width* (num2 - 1) + num;
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) && GetAlpha(lineRaw, num4) == 255) {
                    queue.Enqueue(num);
                    queue2.Enqueue(num2 - 1);
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4] = (byte)mark;
                    if(num > maxX) maxX = num;
                    if(num < minX) minX = num;
                    if(num2-1 > maxY) maxY = num2-1;
                    if(num2-1 < minY) minY = num2-1;
                }
            }
            if (num + 1 < width) {
                int num3 = width * num2 + (num + 1);
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) &&  GetAlpha(lineRaw, num4) == 255) {
                    queue.Enqueue(num+1);
                    queue2.Enqueue(num2);
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4] = (byte)mark;
                    if(num+1 > maxX) maxX = num;
                    if(num+1 < minX) minX = num;
                    if(num2 > maxY) maxY = num2-1;
                    if(num2 < minY) minY = num2-1;
                }
            }
            if (num - 1 > -1) {
                int num3 = width * num2 + (num - 1);
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) &&  GetAlpha(lineRaw, num4) == 255) {
                    queue.Enqueue(num-1);
                    queue2.Enqueue(num2);
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4] = (byte)mark;
                    if(num-1 > maxX) maxX = num;
                    if(num-1 < minX) minX = num;
                    if(num2 > maxY) maxY = num2-1;
                    if(num2 < minY) minY = num2-1;
                }
            }
            if (num2 + 1 < height) {
                int num3 = width * (num2 + 1) + num;
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) &&  GetAlpha(lineRaw, num4) == 255) {
                    queue.Enqueue(num);
                    queue2.Enqueue(num2+1);
                    ++count;
                    tmpPixels.Add(num3);
                    lineRaw[num4] = (byte)mark;
                    if(num > maxX) maxX = num;
                    if(num < minX) minX = num;
                    if(num2+1 > maxY) maxY = num2-1;
                    if(num2+1 < minY) minY = num2-1;
                }
            }
        }
        if(count > minPixels) {
            var clr = new Color32(r, g, b, 255);
            var pi = new PixelsInfo();
            pi.clr = clr;
            pi.pixels = tmpPixels;
            pi.pixelsId = pixelsIdCount;
            pixelsList.Add(pi);
            pixelsIdCount++;
            pi.x = minX;
            pi.y = minY;
            pi.row = maxX - minX + 1;
            pi.col = maxY - minY + 1;
            foreach(var p in tmpPixels) {
                queryList[p] = pi.pixelsId;
            }
        } else {
            smallPixels++;
            foreach(var p in tmpPixels) {
                SetColor(lineRaw, p*4, 0, 0, 0, 255);
            }
        }
    }
    private void FloodFillRemoveBlack(int x, int y, byte[] lineRaw)
    {
        int offset = width * y + x;
        byte r = lineRaw[4 * offset + 1];
        byte g = lineRaw[4 * offset + 2];
        byte b = lineRaw[4 * offset + 3];
        Queue<int> queue = new Queue<int>();
        Queue<int> queue2 = new Queue<int>();
        List<int> tmpPixels = new List<int>();
        queue.Enqueue(x);
        queue2.Enqueue(y);
        int count = 0;
        int mark = 10;
        int maxX = 0;
        int minX = int.MaxValue;
        int maxY = 0;
        int minY = int.MaxValue;
        while (queue.Count > 0) {
            int num = queue.Dequeue();
            int num2 = queue2.Dequeue();
            if (num2 - 1 > -1) {
                int num3 = width* (num2 - 1) + num;
                int num4 = num3 * 4;
                if( (CheckColor(lineRaw, num4, r, g, b) && GetAlpha(lineRaw, num4) > 10) || CheckColor(lineRaw, num4, 0, 0, 0)) {
                    queue.Enqueue(num);
                    queue2.Enqueue(num2 - 1);
                    ++count;
                    tmpPixels.Add(num3);
                    if(CheckColor(lineRaw, num4, 0, 0, 0)) {
                        lineRaw[num4 + 1] = r;
                        lineRaw[num4 + 2] = g;
                        lineRaw[num4 + 3] = b;
                    }
                    lineRaw[num4] = (byte)mark;
                    if(num > maxX) maxX = num;
                    if(num < minX) minX = num;
                    if(num2-1 > maxY) maxY = num2-1;
                    if(num2-1 < minY) minY = num2-1;
                }
            }
            if (num + 1 < width) {
                int num3 = width * num2 + (num + 1);
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) &&  GetAlpha(lineRaw, num4) > 10 || CheckColor(lineRaw, num4, 0, 0, 0)) {
                    queue.Enqueue(num+1);
                    queue2.Enqueue(num2);
                    ++count;
                    tmpPixels.Add(num3);
                    if(CheckColor(lineRaw, num4, 0, 0, 0)) {
                        lineRaw[num4 + 1] = r;
                        lineRaw[num4 + 2] = g;
                        lineRaw[num4 + 3] = b;
                    }
                    lineRaw[num4] = (byte)mark;
                    if(num+1 > maxX) maxX = num;
                    if(num+1 < minX) minX = num;
                    if(num2 > maxY) maxY = num2-1;
                    if(num2 < minY) minY = num2-1;
                }
            }
            if (num - 1 > -1) {
                int num3 = width * num2 + (num - 1);
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) &&  GetAlpha(lineRaw, num4) > 10|| CheckColor(lineRaw, num4, 0, 0, 0)) {
                    queue.Enqueue(num-1);
                    queue2.Enqueue(num2);
                    ++count;
                    tmpPixels.Add(num3);
                    if(CheckColor(lineRaw, num4, 0, 0, 0)) {
                        lineRaw[num4 + 1] = r;
                        lineRaw[num4 + 2] = g;
                        lineRaw[num4 + 3] = b;
                    }
                    lineRaw[num4] = (byte)mark;
                    if(num-1 > maxX) maxX = num;
                    if(num-1 < minX) minX = num;
                    if(num2 > maxY) maxY = num2-1;
                    if(num2 < minY) minY = num2-1;
                }
            }
            if (num2 + 1 < height) {
                int num3 = width * (num2 + 1) + num;
                int num4 = num3 * 4;
                if( CheckColor(lineRaw, num4, r, g, b) &&  GetAlpha(lineRaw, num4) > 10 || CheckColor(lineRaw, num4, 0, 0, 0)) {
                    queue.Enqueue(num);
                    queue2.Enqueue(num2+1);
                    ++count;
                    tmpPixels.Add(num3);
                    if(CheckColor(lineRaw, num4, 0, 0, 0)) {
                        lineRaw[num4 + 1] = r;
                        lineRaw[num4 + 2] = g;
                        lineRaw[num4 + 3] = b;
                    }
                    lineRaw[num4] = (byte)mark;
                    if(num > maxX) maxX = num;
                    if(num < minX) minX = num;
                    if(num2+1 > maxY) maxY = num2-1;
                    if(num2+1 < minY) minY = num2-1;
                }
            }
        }
        if(count > minPixels) {
            var clr = new Color32(r, g, b, 255);
            var pi = new PixelsInfo();
            pi.clr = clr;
            pi.pixels = tmpPixels;
            pi.pixelsId = pixelsIdCount;
            pixelsList.Add(pi);
            pixelsIdCount++;
            pi.x = minX;
            pi.y = minY;
            pi.row = maxX - minX + 1;
            pi.col = maxY - minY + 1;
            foreach(var p in tmpPixels) {
                queryList[p] = pi.pixelsId;
            }
        }
    }
    private Texture2D LoadTexture(string path)
    {
        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        fs.Seek(0, SeekOrigin.Begin);
        byte[] bytes = new byte[fs.Length];//生命字节，用来存储读取到的图片字节
        try {
            fs.Read(bytes, 0, bytes.Length);//开始读取，这里最好用trycatch语句，防止读取失败报错
 
        } catch (Exception e) {
            UnityEngine.Debug.LogError("源图片读取失败 ! path = " + path + ", err = " + e);
            return null;
        }
        fs.Close();
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        if (texture.LoadImage(bytes)) return texture;
        return null;
    }
    private bool CheckColor(byte[] srcRaw, byte[] destRaw, int offset)
    {
        if(srcRaw[offset+1] == destRaw[offset+1] && srcRaw[offset+2] == destRaw[offset+2] && srcRaw[offset+3] == destRaw[offset+3]) return true;
        return false;
    }
    private bool CheckColor(byte[] raw, int offset, byte r, byte g, byte b)
    {
        if(raw[offset+1] == r && raw[offset+2] == g && raw[offset+3] == b) return true;
        return false;
    }
    private bool CheckColorNew(byte[] raw, int offset, byte r, byte g, byte b)
    {
        float mean = (float)(raw[offset + 1] + r) / 2;
        float cr = (float)(raw[offset+1] - r);
        float cg = (float)(raw[offset+2] - g);
        float cb = (float)(raw[offset+3] - b);
        float dis = (2 + mean/256) * cr * cr + 4 * cg * cg + (2 + (255-mean)/256) * cb * cb;
        return dis < 10000;
    }
    private byte GetAlpha(byte[] raw, int offset)
    {
        return raw[offset];
    }
    private void GenerateInnerBox(byte[] lineRaw, ref int smallBox)
    {
        foreach(var pi in pixelsList) {
            GenerateInner(pi, lineRaw, ref smallBox);
        }
    }
    private void SetColor(byte[] raw, int offset, byte r, byte g, byte b, byte a)
    {
        raw[offset] = a;
        raw[offset + 1] = r;
        raw[offset + 2] = g;
        raw[offset + 3] = b;
    }
    private void GenerateInner(PixelsInfo pixels, byte[] lineRaw, ref int smallBox)
    {
        int mx = 0;
        int my = 0;
        int box = 0;
        int[,] dp = new int[pixels.row, pixels.col];
        for(int i = 0; i < pixels.row; ++i) {
            for(int j = 0; j < pixels.col; ++j) {
                int o = pixels.x + i + (pixels.y + j) * width;
                if(CheckList(pixels.pixelsId, o)) {
                    if(i == 0 || j == 0) {
                        dp[i,j] = 1;
                    } else {
                        dp[i,j] = Mathf.Min(dp[i-1, j-1], Math.Min(dp[i-1, j], dp[i, j-1])) + 1;
                    }
                    if(dp[i,j] > box) {
                        mx =  pixels.x + i;
                        my = pixels.y + j;
                        box = dp[i,j];
                    }
                }
            }
        }
        if(box >= minBox) {
            ZoneInfo zi = new ZoneInfo();
            zi.zoneId = zoneCount;
            ++zoneCount;
            zi.x = mx - box / 2;
            zi.y = height - (my - box / 2);
            float bound = (float)box/1.5f == 0 ? 1 : (float)box/1.5f;
            zi.size = bound > 100 ? 100 : (int)bound;
            zi.sketchColor = pixels.clr;
            zi.pixels = pixels.pixels;
            if(!sketchList.Contains(zi.sketchColor)) {
                sketchList.Add(zi.sketchColor);
            }
            zoneList.Add(zi);
        } else {
            foreach(var p in pixels.pixels) {
                SetColor(lineRaw, p*4, 0, 0, 0, 255);
            }
            smallBox++;
        }
    }
    private void GenerateJson(string level, ref int sketch, ref int zone)
    {
        // System.Random rd = new System.Random();
        // int index = 0;
        // Color32 temp;
        // for (int i = 0; i < sketchList.Count; i++)
        // {
        //     index = rd.Next(0, sketchList.Count - 1);
        //     if (index != i)
        //     {
        //         temp = sketchList[i];
        //         sketchList[i] = sketchList[index];
        //         sketchList[index] = temp;
        //     }
        // }
        PngJsonData pjd = new PngJsonData();
        for(int i = 0; i < sketchList.Count; ++i) {
            SketchJsonData sjd = new SketchJsonData();
            sjd.id = i + 1;
            Color32 clr = sketchList[i];
            sjd.r = clr.r;
            sjd.g = clr.g;
            sjd.b = clr.b;
            pjd.sketches.Add(sjd);
        }
        foreach(var zi in zoneList) {
            zi.sketchId = sketchList.IndexOf(zi.sketchColor) + 1;
            ZoneJsonData zjd = new ZoneJsonData();
            zjd.centerX = zi.x;
            zjd.centerY = zi.y;
            zjd.diameter = zi.size;
            zjd.sketchId = zi.sketchId;
            zjd.uid = zi.zoneId;
            pjd.sketches[zjd.sketchId-1].zones.Add(zjd.uid);
            pjd.zones.Add(zjd);
        }
        sketch = sketchList.Count;
        zone = zoneList.Count;
        SaveJson<PngJsonData>(pjd, output + "/" + level + "/", level +  "_mark.json");
    }
    private void GenerateFakeNew(string level, byte[] lineRaw)
    {
        foreach(var z in zoneList) {
            Color32 clr = GenerateColor(z.sketchId, z.zoneId);
            foreach(var p in z.pixels) {
                lineRaw[p * 4] = 255;
                lineRaw[p * 4 + 1] = clr.r;
                lineRaw[p * 4 + 2] = clr.g;
                lineRaw[p * 4 + 3] = clr.b; 
            }
        }
        for(int x = 0; x < width; ++x) {
            for(int y = height - 1; y >= 0; --y) {
                int i = y*width + x;
                lineRaw[i*4] = 255;
            }
        }

        for(int x = 0; x < width; ++x) {
            for(int y = height - 1; y >= 0; --y) {
                int i = y*width + x;
                if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                FloodFillNew(x, y, lineRaw);
            }
        }
        for(int x = 0; x < width; ++x) {
            for(int y = height - 1; y >= 0; --y) {
                int i = y*width + x;
                lineRaw[i*4] = 255;
            }
        }
        for(int x = 0; x < width; ++x) {
            for(int y = height - 1; y >= 0; --y) {
                int i = y*width + x;
                if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                FloodFillNew(x, y, lineRaw);
            }
        }
        for(int x = 0; x < width; ++x) {
            for(int y = height - 1; y >= 0; --y) {
                int i = y*width + x;
                lineRaw[i*4] = 255;
            }
        }
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.LoadRawTextureData(lineRaw);
        byte[] savePngBytes = texture.EncodeToPNG();
        SavePng(savePngBytes, output + "/" + level, level + "_zone.png");
    }
    private Color32 GenerateColor(int sketchId, int zoneId)
    {
        int num = (zoneId * 300 + sketchId) * 10;

        int b = num / 65536;
        int g = (num - b * 65536)/ 256;
        int r = num % 256;

        return new Color32((byte)r, (byte)g, (byte)b, 255);
    }
    public  int RGB2SketchIndex(int r, int g, int b)
    {
        return (b * 256 * 256 + g * 256 + r) / 10 % 300;
    }
    public  int RGB2ZoneUid(int r, int g, int b)
    {
        return (b * 256 * 256 + g * 256 + r) / 10 / 300;
    }
    private  void SavePng(byte[] itemBGBytes, string dir, string file)
    {
        try {
            if(!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            var path = Path.Combine(dir, file);
            File.WriteAllBytes(path, itemBGBytes);
        } catch(IOException e) {
            UnityEngine.Debug.Log("Save Png error + " + " msg = " + e.Message + ", path = " + dir + "/" + file);
            return;
        }
    }
    private void SaveJson<T>(T data, string dir, string file)
    {
        try {
            if(!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            var path = Path.Combine(dir, file);
            var dataJson = LitJson.JsonMapper.ToJson(data);
            using(StreamWriter f = new StreamWriter(path)) {
                string encrypted = dataJson;
                f.Write(encrypted);
            }
        } catch(Exception) {
            return;
        }
    }
    private bool CheckList(int pixelsId, int offset)
    {
        if(offset < 0 || offset >= queryList.Length) return false;
        if(queryList[offset] == pixelsId) return true;
        return false;
    }
    private int GetStableHashCode(string str)
    {
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for(int i = 0; i < str.Length && str[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1 || str[i+1] == '\0')
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i+1];
            }

            return hash1 + (hash2*1566083941);
        }
    }
    public void OpenInput()
    {
        Application.OpenURL(@"file://" + inputField.text);
    }
    public void OpenOutput()
    {
        Application.OpenURL(@"file://" + outputField.text);
    }
}
