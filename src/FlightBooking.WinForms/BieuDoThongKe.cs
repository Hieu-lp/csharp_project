using FlightBooking.DTO;
using System.Drawing.Drawing2D;
namespace FlightBooking.WinForms;
// Vẽ bằng WinForms/GDI+ sẵn có, không thêm dependency hoặc license.
public sealed class BieuDoThongKe : Control
{
    [System.ComponentModel.DefaultValue(false)]
    public bool HinhTron { get; init; }
    [System.ComponentModel.DefaultValue(false)]
    public bool TheoQuy { get; set; }
    private List<DiemBieuDoDTO> duLieu=new();
    private readonly ToolTip goiY=new();
    private readonly List<(RectangleF Vung,string Text)> vung=new();
    private readonly Color[] mau={Color.RoyalBlue,Color.SeaGreen,Color.DarkOrange,Color.MediumPurple,Color.Teal,Color.Crimson,Color.SlateGray};
    public BieuDoThongKe(){DoubleBuffered=true;BackColor=Color.White;Dock=DockStyle.Fill;MinimumSize=new(400,330);MouseMove+=(_,e)=>goiY.SetToolTip(this,vung.FirstOrDefault(x=>x.Vung.Contains(e.Location)).Text??"");}
    public void GanDuLieu(List<DiemBieuDoDTO> data){duLieu=data;Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)goiY.Dispose();base.Dispose(disposing);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;vung.Clear();
        using var chu=new SolidBrush(Color.FromArgb(30,41,59));
        g.DrawString(HinhTron?"Cơ cấu tiền doanh thu PAID theo hạng vé":(TheoQuy?"Doanh thu PAID theo quý · VND":"Doanh thu PAID theo tháng · VND"),Font,chu,16,12);
        if(duLieu.Count==0 || duLieu.All(x=>x.GiaTri==0)){g.DrawString("Không có doanh thu trong bộ lọc đã tải.",Font,chu,20,65);return;}
        if(HinhTron)
        {
            float size=Math.Min(Height-90,Width*0.4f),angle=-90;var circle=new RectangleF(20,55,size,size);
            for(int i=0;i<duLieu.Count;i++)
            {
                var d=duLieu[i];float sweep=(float)(d.TyLe*3.6m);using var b=new SolidBrush(mau[i%mau.Length]);
                g.FillPie(b,circle.X,circle.Y,circle.Width,circle.Height,angle,sweep);angle+=sweep;
                var rect=new RectangleF(size+42,58+i*44,Width-size-55,42);g.FillRectangle(b,rect.X,rect.Y+3,12,12);
                string text=$"{d.Nhan}: {d.GiaTri:N0} đ ({d.TyLe:N2}%)";
                using var fmt=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.LineLimit};
                using var fontChuGiai=new Font(Font.FontFamily,9F);
                g.DrawString(d.Nhan,fontChuGiai,chu,new RectangleF(rect.X+19,rect.Y,rect.Width-19,20),fmt);
                g.DrawString($"{d.GiaTri:N0} VND · {d.TyLe:N2}%",fontChuGiai,chu,new RectangleF(rect.X+19,rect.Y+21,rect.Width-19,20),fmt);vung.Add((rect,text));
            }
            vung.Add((circle,string.Join("\n",duLieu.Select(x=>$"{x.Nhan}: {x.GiaTri:N0} đ ({x.TyLe:N2}%)"))));
        }
        else
        {
            float left=105,top=60,bottom=Height-70,w=Width-left-24,h=bottom-top;
            decimal min=Math.Min(0,duLieu.Min(x=>x.GiaTri)),max=Math.Max(0,duLieu.Max(x=>x.GiaTri));decimal range=max-min;
            float Y(decimal value)=>bottom-(float)((value-min)/range)*h;
            using var pen=new Pen(Color.LightGray);float zero=Y(0);g.DrawLine(pen,left,zero,Width-20,zero);
            for(int n=0;n<=4;n++){decimal value=min+range*n/4;float y=Y(value);g.DrawLine(pen,left,y,Width-20,y);g.DrawString(value.ToString("N0"),Font,chu,3,y-8);}
            float step=w/duLieu.Count;
            for(int i=0;i<duLieu.Count;i++)
            {
                var d=duLieu[i];float y=Y(d.GiaTri);var rect=new RectangleF(left+i*step+step*.15f,Math.Min(y,zero),step*.7f,Math.Abs(zero-y));
                using var b=new SolidBrush(d.GiaTri<0?Color.Crimson:Color.RoyalBlue);g.FillRectangle(b,rect);vung.Add((new RectangleF(left+i*step,top,step,h),$"{d.Nhan}: {d.GiaTri:N0} VND"));
                if(i%Math.Max(1,duLieu.Count/8)==0)g.DrawString(d.Nhan,Font,chu,left+i*step,bottom+10);
            }
            g.DrawString((TheoQuy?"Kỳ (quý/năm) · cột xanh: thu PAID":"Kỳ (tháng/năm) · cột xanh: thu PAID"),Font,chu,left,Height-27);
        }
    }
}
