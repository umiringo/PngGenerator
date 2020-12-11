using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;

public class PngGenerator : MonoBehaviour
{
    #pragma warning disable 0649
        [SerializeField] string input;
        [SerializeField] string output;
        [SerializeField] int alphaLimit;
        [SerializeField] int alphaLow;
        [SerializeField] int alphaHigh;
        [SerializeField] int minSize;
        [SerializeField] int pixelSize;
    #pragma warning restore 0649

    public class ZoneInfo 
    {
        public int zoneId;
        public Color32 sketchColor;
        public int sketchId;
        public int size;
        public int x;
        public int y;
        public List<int> pixels = new List<int>();
        public int bx;
        public int by;
        public int box;
    }
    public class PixelsInfo 
    {
        public int pixelsId;
        public Color32 clr;
        public List<int> pixels = new List<int>();
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
       GenerateResourceNew("/Users/umiringo/Project/PngGenerator/Input/test/line.png");
       //RemoveAllAlpha("/Users/umiringo/Project/PngGenerator/Input/test2/line.png");
    }
    private void RemoveAllAlpha(string linePath)
    {
        Texture2D lineTex = LoadTexture(linePath);
        if(lineTex == null) {
            if(lineTex == null) UnityEngine.Debug.LogError("Generator Failed, blockPath = " + linePath);
            return;
        }
        byte[] lineRaw = lineTex.GetRawTextureData();
        UnityEngine.Debug.Log("Scan Start... line size = " + lineRaw.Length);
        for(int i = 0; i < lineRaw.Length/4; ++i) {
            if(lineRaw[i*4] < 140 && lineRaw[i*4] > 0) {
                //UnityEngine.Debug.Log("fuck alpha = " + lineRaw[i*4]);
                lineRaw[i*4] = 0;
            } else if(lineRaw[i*4] > 140) {
                lineRaw[i*4] = 255;
            }
        }
        Texture2D texture= new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.LoadRawTextureData(lineRaw);
        byte[] savePngBytes = texture.EncodeToPNG();
        SavePng(savePngBytes, output, "test2_line.png");
        UnityEngine.Debug.Log("Scan Finished.");
    }
    public void GenerateResourceNew(string linePath)
    {
        UnityEngine.Debug.Log("Generating Start...");
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // 初始化
        zoneCount = 1;
        pixelsIdCount = 1;
        pixelsList.Clear();
        zoneList.Clear();
        sketchList.Clear();
        queryList = new int[width * height];
        // 先讀取文件
        Texture2D lineTex = LoadTexture(linePath);
        if(lineTex == null) {
            if(lineTex == null) UnityEngine.Debug.LogError("Generator Failed, blockPath = " + linePath);
            return;
        }
        // 圖片生成為rawdata
        byte[] lineRaw = lineTex.GetRawTextureData();
        UnityEngine.Debug.Log("Scan Start... line size = " + lineRaw.Length);
        for(int x = 0; x < width; ++x) {
            for(int y = 0; y < height; ++y) {
                int i = y*width + x;
                if(GetAlpha(lineRaw, i*4) < 255) continue; // 已经统计过了
                if(CheckColor(lineRaw, i*4, 0, 0, 0)) continue;
                FloodFillNew(x, y, lineRaw);
            }
        }
        // 對已經記錄的表進行掃描，寻找最小内切正方形, 確定坐標和size
        GenerateInnerBox(lineRaw);
        //根據結果生成json文件
        GenerateJson();
        //根據結果從新計算顔色生成假圖
        GenerateFakeNew(lineRaw);
        sw.Stop();
        UnityEngine.Debug.Log("Generating End... time = " + sw.ElapsedMilliseconds);
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
        int mark = 138;
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
                }
            }
        }
        if(count > pixelSize) {
            var clr = new Color32(r, g, b, 255);
            var pi = new PixelsInfo();
            pi.clr = clr;
            pi.pixels = tmpPixels;
            pi.pixelsId = pixelsIdCount;
            pixelsList.Add(pi);
            pixelsIdCount++;
            foreach(var p in tmpPixels) {
                queryList[p] = pi.pixelsId;
            }
        } else {
            //UnityEngine.Debug.Log("[FloodFill] small area! size = " + count);
        }
    }
    public void GenerateResource(string blockPath, string linePath)
    {
        UnityEngine.Debug.Log("Generating Start...");
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // 初始化
        zoneCount = 1;
        pixelsIdCount = 1;
        pixelsList.Clear();
        zoneList.Clear();
        sketchList.Clear();
        queryList = new int[width * height];
        // 先讀取文件
        Texture2D blockTex = LoadTexture(blockPath);
        Texture2D lineTex = LoadTexture(linePath);
        if(blockTex == null || lineTex == null) {
            if(blockTex == null) UnityEngine.Debug.LogError("Generator Failed, blockPath = " + blockPath);
            if(lineTex == null) UnityEngine.Debug.LogError("Generator Failed, blockPath = " + linePath);
            return;
        }
        // 圖片生成為rawdata
        byte[] blockRaw = blockTex.GetRawTextureData();
        byte[] lineRaw = lineTex.GetRawTextureData();
        UnityEngine.Debug.Log("Scan Start... block size = " + blockRaw.Length + ", line size = " + lineRaw.Length);
        for(int x = 0; x < width; ++x) {
            for(int y = 0; y < height; ++y) {
                int i = y*width + x;
                if(GetAlpha(lineRaw, i * 4) > 0 || GetAlpha(blockRaw, i*4) < 255) continue; // 已经统计过了
                FloodFill(x, y, blockRaw, lineRaw);
            }
        }
        // 對已經記錄的表進行掃描，寻找最小内切正方形, 確定坐標和size
        GenerateInnerBox(lineRaw);
        //根據結果生成json文件
        GenerateJson();
        //根據結果從新計算顔色生成假圖
        GenerateFake(blockRaw, lineRaw);
        sw.Stop();
        UnityEngine.Debug.Log("Generating End... time = " + sw.ElapsedMilliseconds);
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
        if (texture.LoadImage(bytes))
        {
            print("图片加载完毕, path = " + path);
            return texture;//将生成的texture2d返回，到这里就得到了外部的图片，可以使用了
 
        } else {
            print("图片尚未加载成功, path = " + path);
            return null;
        }
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
    private byte GetAlpha(byte[] raw, int offset)
    {
        return raw[offset];
    }
    private void FloodFill(int x, int y, byte[] blockRaw, byte[] lineRaw)
    {
        int offset = width * y + x;
        byte r = blockRaw[4 * offset + 1];
        byte g = blockRaw[4 * offset + 2];
        byte b = blockRaw[4 * offset + 3];
        Queue<int> queue = new Queue<int>();
        Queue<int> queue2 = new Queue<int>();
        List<int> tmpPixels = new List<int>();
        queue.Enqueue(x);
        queue2.Enqueue(y);
        int count = 0;
        int mark = 138;
        while (queue.Count > 0) {
            int num = queue.Dequeue();
            int num2 = queue2.Dequeue();
            if (num2 - 1 > -1) {
                int num3 = width* (num2 - 1) + num;
                int num4 = num3 * 4;
                byte al = GetAlpha(lineRaw, num4);
                byte ab = GetAlpha(blockRaw, num4);
                bool cb = CheckColor(blockRaw, num4, r, g, b);
                bool cl = al > 0 && al < alphaLimit;
                if( (cb && ab != mark) || (!cb && cl)) {
                    queue.Enqueue(num);
                    queue2.Enqueue(num2 - 1);
                    ++count;
                    tmpPixels.Add(num3);
                    blockRaw[num4+1] = r;
                    blockRaw[num4+2] = g;
                    blockRaw[num4+3] = b;
                    blockRaw[num4] = (byte)mark;
                }
            }
            if (num + 1 < width) {
                int num3 = width * num2 + (num + 1);
                int num4 = num3 * 4;
                byte al = GetAlpha(lineRaw, num4);
                byte ab = GetAlpha(blockRaw, num4);
                bool cb = CheckColor(blockRaw, num4, r, g, b);
                bool cl = al > 0 && al < alphaLimit;
                if( (cb && ab != mark) || (!cb && cl)) {
                    queue.Enqueue(num+1);
                    queue2.Enqueue(num2);
                    ++count;
                    tmpPixels.Add(num3);
                    blockRaw[num4+1] = r;
                    blockRaw[num4+2] = g;
                    blockRaw[num4+3] = b;
                    blockRaw[num4] = (byte)mark;
                }
            }
            if (num - 1 > -1) {
                int num3 = width * num2 + (num - 1);
                int num4 = num3 * 4;
                byte al = GetAlpha(lineRaw, num4);
                byte ab = GetAlpha(blockRaw, num4);
                bool cb = CheckColor(blockRaw, num4, r, g, b);
                bool cl = al > 0 && al < alphaLimit;
                if( (cb && ab != mark) || (!cb && cl)) {
                    queue.Enqueue(num-1);
                    queue2.Enqueue(num2);
                    ++count;
                    tmpPixels.Add(num3);
                    blockRaw[num4+1] = r;
                    blockRaw[num4+2] = g;
                    blockRaw[num4+3] = b;
                    blockRaw[num4] = (byte)mark;
                }
            }
            if (num2 + 1 < height) {
                int num3 = width * (num2 + 1) + num;
                int num4 = num3 * 4;
                byte al = GetAlpha(lineRaw, num4);
                byte ab = GetAlpha(blockRaw, num4);
                bool cb = CheckColor(blockRaw, num4, r, g, b);
                bool cl = al > 0 && al < alphaLimit;
                if( (cb && ab != mark) || (!cb && cl)) {
                    queue.Enqueue(num);
                    queue2.Enqueue(num2 + 1);
                    ++count;
                    tmpPixels.Add(num3);
                    blockRaw[num4+1] = r;
                    blockRaw[num4+2] = g;
                    blockRaw[num4+3] = b;
                    blockRaw[num4] = (byte)mark;
                }
            }
        }
        if(count > pixelSize) {
            var clr = new Color32(r, g, b, 255);
            var pi = new PixelsInfo();
            pi.clr = clr;
            pi.pixels = tmpPixels;
            pi.pixelsId = pixelsIdCount;
            pixelsList.Add(pi);
            pixelsIdCount++;
            foreach(var p in tmpPixels) {
                queryList[p] = pi.pixelsId;
            }
        } else {
            foreach(var tp in tmpPixels) {
                lineRaw[tp * 4] = 255;
                lineRaw[tp * 4 + 1] = 0;
                lineRaw[tp * 4 + 2] = 0;
                lineRaw[tp * 4 + 3] = 0;
            }
        }
    }
    private void GenerateInnerBox(byte[] lineRaw)
    {
        //UnityEngine.Debug.Log("PixelsList = " + pixelsList.Count);
        foreach(var pi in pixelsList) {
            GenerateInner(pi, lineRaw);
        }
    }
    private void GenerateInner(PixelsInfo pixels, byte[] lineRaw)
    {
        int mx = 0;
        int my = 0;
        int box = 0;
        foreach(var p in pixels.pixels) {
            int w = 0;
            int h = 0;
            int i = 1;
            // while(true) {
            //     if(CheckList(pixels.pixelsId, p + i)) {
            //         w++;
            //         i++;
            //     } else {
            //         break;
            //     }
            // }
            // i = 1;
            // while(true) {
            //     if(CheckList(pixels.pixelsId, p + i*width)) {
            //         h++;
            //         i++;
            //     } else {
            //         break;
            //     }
            // }
            int k = 1;
            while(true) {
                for(int m = 0; m < k; ++m) {
                    if(!CheckList(pixels.pixelsId, p + k + m * width)) break;
                    if(!CheckList(pixels.pixelsId, p + m + k * width)) break;
                }
                k++;
            }
            int low = k - 1;
            if(low > box) {
                mx = p % width;
                my = p / width;
                box = low;
            }
        }
        if(box > minSize) {
            ZoneInfo zi = new ZoneInfo();
            zi.zoneId = zoneCount;
            ++zoneCount;
            zi.x = mx + box / 2;
            zi.y = height - (my + box / 2);
            zi.bx = mx;
            zi.by = my;
            zi.box = box;
            if(zi.x > width) UnityEngine.Debug.LogWarning("x out of bound!");
            if(zi.y > height) UnityEngine.Debug.LogWarning("y out of bound! my = " + my + ", box = " + box);
            var bound = box/2 == 0 ? 1 : box/2;
            zi.size = bound > 80 ? 80 : bound;
            zi.sketchColor = pixels.clr;
            zi.pixels = pixels.pixels;
            if(!sketchList.Contains(zi.sketchColor)) {
                sketchList.Add(zi.sketchColor);
            }
            zoneList.Add(zi);
        } else {
            UnityEngine.Debug.Log("[InnerBox] small inner box area! box = " + box);
        }
    }
    private void GenerateJson()
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
        UnityEngine.Debug.Log("Sketch Count = " + sketchList.Count);
        UnityEngine.Debug.Log("Zone Count = " + zoneList.Count);
        SaveJson<PngJsonData>(pjd, output + "/mark.json");
    }
    private void GenerateFakeNew(byte[] lineRaw)
    {
        foreach(var z in zoneList) {
            Color32 clr = GenerateColor(z.sketchId, z.zoneId);
            // foreach(var p in z.pixels) {
            //     lineRaw[p * 4] = 255;
            //     lineRaw[p * 4 + 1] = clr.r;
            //     lineRaw[p * 4 + 2] = clr.g;
            //     lineRaw[p * 4 + 3] = clr.b; 
            // }
            for(int i = 0; i < z.box; ++i) {
                for(int j = 0; j < z.box; ++j) {
                    int offset = (z.bx + i) + (z.by + j) * width;
                    lineRaw[offset*4] = 255;
                    lineRaw[offset*4+1] = clr.r;
                    lineRaw[offset*4+2] = clr.g;
                    lineRaw[offset*4+3] = clr.b;
                }
            }
        }
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.LoadRawTextureData(lineRaw);
        byte[] savePngBytes = texture.EncodeToPNG();
        SavePng(savePngBytes, output, "fake.png");
    }
    private void GenerateFake(byte[] blockRaw, byte[] lineRaw)
    {
        foreach(var z in zoneList) {
            Color32 clr = GenerateColor(z.sketchId, z.zoneId);
            foreach(var p in z.pixels) {
                blockRaw[p * 4] = 255;
                blockRaw[p * 4 + 1] = clr.r;
                blockRaw[p * 4 + 2] = clr.g;
                blockRaw[p * 4 + 3] = clr.b; 
            }
        }
        for(int i = 0; i < lineRaw.Length / 4; ++i) {
            if(lineRaw[i*4] > alphaLimit) {
                blockRaw[i*4] = 255;
                blockRaw[i*4 + 1] = 0;
                blockRaw[i*4 + 2] = 0;
                blockRaw[i*4 + 3] = 0;
            }
        }
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.LoadRawTextureData(blockRaw);
        byte[] savePngBytes = texture.EncodeToPNG();
        SavePng(savePngBytes, output, "fake.png");

        Texture2D texture2= new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture2.filterMode = FilterMode.Point;
        texture2.LoadRawTextureData(lineRaw);
        byte[] savePngBytes2 = texture2.EncodeToPNG();
        SavePng(savePngBytes2, output, "new_line.png");
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
    private void SaveJson<T>(T data, string path)
    {
        try {
            var dataJson = LitJson.JsonMapper.ToJson(data);
            using(StreamWriter file = new StreamWriter(path)) {
                string encrypted = dataJson;
                file.Write(encrypted);
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
}
