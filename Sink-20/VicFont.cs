using System.IO;
using System.Drawing;

namespace Sink_20
{
   static class VicFont
   {
      public static Bitmap[] sChars = new Bitmap[256];
      public static Bitmap sOutline;

      public static void WriteLine( Graphics g, string line, int x, int y, int scale )
      {
         for ( int i = 0; i < line.Length; ++i )
         {
            g.DrawImage( sChars[Tokenizer.CharToCode( line[i] )], x + 8 * i * scale, y, 8 * scale, 8 * scale );
         }
      }

      public static Bitmap GetColouredCharacter( int code, Color color )
      {
         Bitmap b = new Bitmap( sChars[code] );
         for ( int y = 0; y < 8; ++y )
         {
            for ( int x = 0; x < 8; ++x )
            {
               b.SetPixel( x, y, b.GetPixel( x, y ).R == 0 ? color : Color.Transparent );
            }
         }
         return b;
      }

      static VicFont()
      {
         byte[] b = File.ReadAllBytes( "chargen" );
         for ( int i = 0; i < 256; ++i )
         {
            Bitmap c = sChars[i] = new Bitmap( 8, 8 );
            for ( int y = 0; y < 8; ++y )
            {
               for ( int x = 0; x < 8; ++x )
               {
                  c.SetPixel( x, y, ( b[8 * i + y] & ( 128 >> x ) ) > 0 ? Color.Black : Color.White );
               }
            }
         }

         sOutline = new Bitmap( 10, 10 );
         for ( int y = 0; y < 10; ++y )
         {
            for ( int x = 0; x < 10; ++x )
            {
               if ( ( x % 9 ) == 0 || ( y % 9 ) == 0 )
               {
                  sOutline.SetPixel( x, y, Color.LightGray );
               }
            }
         }

      }
   }
}
