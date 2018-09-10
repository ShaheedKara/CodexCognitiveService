
using System.Drawing;
using System.Linq;
using System.Web;

//This class controls the colour of the text @the handwriting part
//uses the drawing namespace not pickford
//not important :)
namespace MicroMk1
{
    public static class Settings
    {
        public static Color[] ImageSquareColors
        {
            get
            {
                return new[] { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Brown, Color.Cyan,
                               Color.DarkBlue, Color.Olive, Color.PaleVioletRed, Color.Pink};
            }
        }
    }
}