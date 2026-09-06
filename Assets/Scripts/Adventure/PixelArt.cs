using System.Collections.Generic;
using UnityEngine;

namespace Alif.Adventure
{
    // A shared 24-pixel world grid keeps furniture, people, and scene art at the same density.
    public static class PixelArt
    {
        public const int Ppu = 24;
        public static readonly Color Ink = Hex("25363d"), Paper = Hex("fff1d2"), Teal = Hex("356b65"), Gold = Hex("f2bf64"), Coral = Hex("cb765b");
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        public static Color Hex(string s) { ColorUtility.TryParseHtmlString("#"+s, out var c); return c; }
        sealed class Draw
        {
            public int W,H; public Color[] Pixels;
            public Draw(int w,int h,Color fill) { W=w; H=h; Pixels=new Color[w*h]; Rect(0,0,w,h,fill); }
            public void Rect(int x,int y,int w,int h,Color c) { for(int j=Mathf.Max(0,y);j<Mathf.Min(H,y+h);j++) for(int i=Mathf.Max(0,x);i<Mathf.Min(W,x+w);i++) Pixels[(H-j-1)*W+i]=c; }
            public void Line(int x,int y,int x2,int y2,Color c) { int n=Mathf.Max(Mathf.Abs(x2-x),Mathf.Abs(y2-y)); for(int i=0;i<=n;i++) Rect(Mathf.RoundToInt(Mathf.Lerp(x,x2,n==0?0:i/(float)n)),Mathf.RoundToInt(Mathf.Lerp(y,y2,n==0?0:i/(float)n)),1,1,c); }
            public Sprite Sprite(string name,Vector2 pivot) { var t=new Texture2D(W,H,TextureFormat.RGBA32,false) { name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };t.SetPixels(Pixels);t.Apply();return UnityEngine.Sprite.Create(t,new Rect(0,0,W,H),pivot,Ppu); }
        }
        static void Window(Draw d,int x,int y,int w=24,int h=24)
        {
            d.Rect(x-2,y-2,w+4,h+4,Ink);d.Rect(x,y,w,h,Hex("77aaa1"));d.Rect(x+2,y+2,w-4,h/2-3,Hex("aad2be"));
            d.Rect(x+w/2-1,y,2,h,Paper);d.Rect(x,y+h/2,w,2,Paper);d.Rect(x-3,y+h,w+6,3,Hex("c28d5c"));
        }
        static void Plant(Draw d,int x,int y,int size=16)
        {
            d.Rect(x-3,y+size/2,6,size/2,Hex("8c654e"));d.Rect(x-size/2,y-size/2,size,size,Hex("3f725b"));d.Rect(x-size/2+2,y-size/2-3,size-4,size,Hex("548568"));d.Rect(x-3,y-size/2-2,size/2,size/2,Hex("8fb078"));
            d.Rect(x-6,y+size/2+3,12,7,Coral);d.Rect(x-8,y+size/2+1,16,3,Hex("e39b70"));
        }
        static void Roof(Draw d,int x,int y,int w,int h,Color color)
        {
            d.Rect(x,y+4,w,h,Hex("824e49"));
            for(int j=0;j<h;j+=6) {d.Rect(x,y+j,w,4,color);for(int i=0;i<w;i+=12)d.Rect(x+i+(j%12==0?0:6),y+j,1,4,Hex("e2a07a"));}
            d.Rect(x-3,y+h,w+6,4,Ink);d.Rect(x-2,y+h,w+4,2,Hex("a96752"));
        }
        public static Sprite Room(int chapter,int area)
        {
            string key=$"room{chapter}.{area}";if(Cache.TryGetValue(key,out var sprite))return sprite;
            bool outdoor=area==0 || (chapter==1&&area==1) || (chapter==3&&area==1) || (chapter==4&&area==1);
            var d=new Draw(384,216,Hex("b8ccba"));
            if(outdoor)
            {
                d.Rect(0,0,384,80,Hex("a8c9bd"));
                for(int x=12;x<384;x+=76) { d.Rect(x,25+(x%3)*5,32,5,Hex("d8dfca"));d.Rect(x+8,21+(x%3)*5,16,5,Hex("d8dfca")); }
                d.Rect(0,112,384,104,Hex("91a274"));d.Rect(12,136,360,63,Hex("bdaa85"));
                for(int y=140;y<199;y+=10) for(int x=14+(y%20);x<370;x+=20){d.Rect(x,y,17,1,Hex("d2c19a"));d.Rect(x,y,1,8,Hex("a79779"));}
                for(int x=4;x<380;x+=19) {d.Rect(x,124+(x%7),3,1,Hex("bcc18b"));d.Rect(x+4,206-(x%5),1,3,Hex("5b825c"));}
                if(chapter==1 && area==0)
                {
                    d.Rect(28,45,328,72,Hex("e3c49b"));Roof(d,18,26,348,24,Coral);
                    for(int x=48;x<350;x+=54)Window(d,x,69,30,28);
                    d.Rect(169,61,47,56,Ink);d.Rect(173,63,39,51,Hex("547c75"));d.Rect(175,66,16,44,Hex("a2bfb0"));d.Rect(194,66,16,44,Hex("769f97"));
                    d.Rect(136,41,112,12,Paper);d.Rect(10,118,364,4,Hex("797f70"));
                    for(int x=40;x<360;x+=90){d.Rect(x,151,25,6,Hex("856854"));d.Rect(x+2,157,3,5,Ink);d.Rect(x+20,157,3,5,Ink);}
                }
                else if(chapter==4 || (chapter==3 && area==1))
                {
                    for(int x=20;x<380;x+=117){d.Rect(x,79,104,46,Hex("ba8c64"));d.Rect(x+5,68,3,59,Ink);d.Rect(x+96,68,3,59,Ink);for(int i=0;i<104;i+=13)d.Rect(x+i,55,13,27,i%26==0?Coral:Paper);d.Rect(x,82,104,4,Ink);d.Rect(x+5,97,94,14,Hex("e5c08c"));for(int i=0;i<80;i+=16)d.Rect(x+12+i,88,10,9,Teal);}
                }
                else if(chapter==5)
                {
                    d.Rect(114,54,157,75,Hex("dfc7a1"));Roof(d,105,31,175,28,Teal);Window(d,126,82);Window(d,235,82);d.Rect(178,77,30,52,Ink);d.Rect(182,80,22,46,Hex("c89a6c"));
                    for(int x=35;x<370;x+=300){Plant(d,x,68,44);Plant(d,x+12,99,23);}
                    d.Rect(145,180,94,18,Hex("718e85"));d.Rect(151,177,82,17,Hex("b4d1c0"));d.Rect(160,182,62,6,Hex("73aaa2"));
                }
                else
                {
                    d.Rect(52,50,280,79,Hex("d9ba90"));Roof(d,42,26,300,29,Coral);
                    for(int x=70;x<326;x+=68)Window(d,x,70,35,31);
                    d.Rect(174,77,38,52,Ink);d.Rect(179,80,28,47,Hex("9b7557"));d.Rect(201,103,3,3,Gold);
                    d.Rect(58,110,104,13,Hex("688b6e"));d.Rect(221,110,104,13,Hex("688b6e"));
                    for(int x=60;x<325;x+=16)d.Rect(x,106,5,8,Hex("91ae79"));
                }
                Plant(d,15,102,24);Plant(d,370,105,24);
            }
            else
            {
                d.Rect(0,0,384,100,Hex("d6bc94"));d.Rect(0,96,384,120,Hex("ad8968"));d.Rect(0,96,384,5,Ink);
                for(int y=104;y<216;y+=12){d.Rect(0,y,384,1,Hex("c9a781"));for(int x=(y%24)*5;x<384;x+=48)d.Rect(x,y,1,12,Hex("8e715a"));}
                d.Rect(8,18,368,77,Hex("e9d4ad"));d.Rect(12,23,360,3,Hex("f8e5bf"));
                Window(d,28,35,49,46);Window(d,305,35,49,46);
                d.Rect(115,36,150,43,Teal);d.Rect(120,41,140,33,Hex("487d70"));
                for(int x=128;x<257;x+=19){d.Rect(x,49,13,2,Hex("d7d3a7"));d.Rect(x,57,10,2,Hex("b8c29d"));}
                Plant(d,16,88,19);Plant(d,366,88,19);
                if(chapter==1){d.Rect(88,85,207,20,Hex("795846"));d.Rect(84,82,215,7,Hex("d9af7c"));for(int x=95;x<290;x+=24){d.Rect(x,77,12,5,Paper);d.Rect(x+3,74,6,4,Gold);}}
                if(chapter==2 && area==2){d.Rect(24,102,65,40,Ink);d.Rect(27,104,59,34,Teal);d.Rect(29,105,55,11,Paper);d.Rect(29,119,55,17,Hex("74a191"));d.Rect(293,105,61,18,Hex("805f4d"));d.Rect(290,102,67,7,Hex("cdab77"));}
                if(chapter==3){for(int x=28;x<370;x+=285){d.Rect(x,90,44,54,Hex("795846"));for(int y=95;y<140;y+=16)for(int i=0;i<35;i+=6)d.Rect(x+4+i,y,4,13,i%12==0?Coral:Gold);}}
                if(chapter==5){d.Rect(112,88,159,19,Hex("936e51"));d.Rect(110,85,163,6,Hex("d0aa76"));for(int x=124;x<260;x+=26){d.Rect(x,82,17,3,Paper);}}
                d.Rect(119,164,145,32,Hex("6a8b77"));d.Rect(123,168,137,24,Hex("8ba187"));d.Rect(127,171,129,1,Hex("b8b996"));
            }
            // Clear, contrasting landing strips are identical at both ends of every connection.
            d.Rect(0,163,31,27,Teal);d.Rect(353,163,31,27,Teal);
            for(int n=0;n<3;n++){d.Line(17-n*4,171,12-n*4,176,Paper);d.Line(12-n*4,176,17-n*4,181,Paper);d.Line(365+n*4,171,370+n*4,176,Paper);d.Line(370+n*4,176,365+n*4,181,Paper);}
            sprite=d.Sprite(key,new Vector2(.5f,.5f));Cache[key]=sprite;return sprite;
        }
        public static Sprite Person(string name,int frame=0,bool back=false)
        {
            string key=name+frame+back;if(Cache.TryGetValue(key,out var s))return s;
            Color shirt=name.Contains("Naya")?Hex("b888a0"):name.Contains("Dimas")?Hex("719397"):name.Contains("Raka")?Coral:name.Contains("Siti")?Hex("b4a467"):name.Contains("Farid")?Paper:Teal;
            var d=new Draw(24,32,Color.clear);
            d.Rect(4,28,16,3,new Color(.1f,.17f,.18f,.3f));
            d.Rect(7,21,4,7,Ink);d.Rect(14,21,4,7,Ink);d.Rect(6,27+(frame%2),6,2,Hex("584b43"));d.Rect(14,28-(frame%2),6,2,Hex("584b43"));
            d.Rect(5,13,15,10,Ink);d.Rect(6,13,13,9,shirt);d.Rect(7,14,3,7,Color.Lerp(shirt,Paper,.23f));
            d.Rect(3,15+frame%2,3,7,Hex("bd8e69"));d.Rect(19,15-frame%2,3,7,Hex("dbab7c"));
            d.Rect(7,2,11,12,Ink);d.Rect(6,4,13,8,Ink);d.Rect(8,5,9,8,Hex("dda878"));d.Rect(9,6,7,5,Hex("e8bb89"));
            if(back)d.Rect(7,3,11,10,Hex("3e3f39"));
            else{d.Rect(8,3,9,3,Hex("3e3f39"));d.Rect(8,7,2,2,Ink);d.Rect(14,7,2,2,Ink);d.Rect(11,11,3,1,Hex("9c6654"));}
            if(name.Contains("Naya")||name.Contains("Siti")){d.Rect(5,3,3,12,shirt);d.Rect(17,3,3,12,shirt);d.Rect(7,1,11,3,shirt);d.Rect(7,13,11,3,shirt);}
            if(name.Contains("Farid")){d.Rect(7,1,11,4,Paper);d.Rect(9,11,7,3,Paper);}
            s=d.Sprite(key,new Vector2(.5f,.1f));Cache[key]=s;return s;
        }
        public static Sprite Object(string kind)
        {
            if(Cache.TryGetValue(kind,out var s))return s;
            var d=new Draw(48,40,Color.clear);
            d.Rect(2,34,44,5,new Color(.12f,.18f,.17f,.22f));
            if(kind=="inspect")
            {
                d.Rect(3,5,42,30,Ink);d.Rect(5,7,38,26,Hex("bb9169"));d.Rect(8,10,17,20,Ink);
                for(int y=12;y<29;y+=3)d.Rect(9,y,15,1,Hex("799087"));d.Rect(29,10,11,6,Hex("94bbad"));d.Rect(32,22,6,6,Ink);d.Rect(34,23,2,3,Gold);d.Line(37,32,40,27,Ink);d.Line(40,27,38,25,Ink);
                d.Line(11,5,16,0,Ink);d.Line(16,0,35,0,Ink);
            }
            else if(kind=="budget")
            {
                d.Rect(4,6,39,29,Ink);d.Rect(6,7,17,25,Paper);d.Rect(25,7,16,25,Hex("e8d9b2"));
                for(int y=12;y<30;y+=5){d.Rect(8,y,12,1,Hex("92aa98"));d.Rect(28,y,10,1,Hex("b29471"));}
                d.Rect(31,21,11,11,Gold);d.Rect(34,24,5,5,Hex("b28b51"));
            }
            else if(kind=="flow")
            {
                d.Rect(2,2,44,33,Ink);d.Rect(4,4,40,29,Teal);d.Rect(8,8,10,7,Paper);d.Rect(30,8,10,7,Paper);d.Rect(19,23,10,7,Gold);
                d.Line(18,11,29,11,Gold);d.Line(34,16,25,22,Gold);d.Line(13,15,22,22,Paper);
            }
            else
            {
                d.Rect(4,3,39,31,Hex("826247"));d.Rect(6,5,35,26,Paper);d.Rect(11,10,25,2,Teal);d.Rect(11,15,21,2,Hex("af9b75"));d.Rect(11,20,24,2,Hex("af9b75"));d.Rect(11,25,14,2,Coral);d.Rect(8,34,3,5,Ink);d.Rect(36,34,3,5,Ink);
            }
            s=d.Sprite(kind,new Vector2(.5f,.12f));Cache[kind]=s;return s;
        }
    }
}
