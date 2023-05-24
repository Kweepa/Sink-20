using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Sink_20
{
   class ScrollableTextBox
   {
      PictureBox mPictureBox;
      VScrollBar mScrollBar;

      public List<List<int>> text;
      public int mZoom;
      public int mMargin;

      static Bitmap sHeart;
      static Bitmap sCaret;
      static Bitmap sHighlight;
      static Bitmap sPurestGreen;

      Bitmap b;

      public ScrollableTextBox( PictureBox _pictureBox, VScrollBar _vScrollBar, int margin )
      {
         mPictureBox = _pictureBox;
         mScrollBar = _vScrollBar;
         text = new List<List<int>>();
         mMargin = margin;
         mZoom = 1;
      }

      public static Bitmap Heart
      {
         get
         {
            if ( sHeart == null )
            {
               sHeart = VicFont.GetColouredCharacter( 83, Color.Red );
            }
            return sHeart;
         }
      }

      public static Bitmap Caret
      {
         get
         {
            if ( sCaret == null )
            {
               sCaret = VicFont.GetColouredCharacter( '>', Color.Red );
            }
            return sCaret;
         }
      }

      public static Bitmap Highlight
      {
         get
         {
            if ( sHighlight == null )
            {
               sHighlight = new Bitmap( 1, 1 );
               sHighlight.SetPixel( 0, 0, Color.FromArgb( 0x3fff0000 ) );
            }
            return sHighlight;
         }
      }

      public static Bitmap PurestGreen
      {
         get
         {
            if ( sPurestGreen == null )
            {
               sPurestGreen = new Bitmap( 1, 1 );
               sPurestGreen.SetPixel( 0, 0, Color.FromArgb( 0x3f00ff00 ) );
            }
            return sPurestGreen;
         }
      }

      public void AddString( string str )
      {
         List<int> line = new List<int>( str.Length );
         for ( int x = 0; x < str.Length; ++x )
         {
            line.Add( Tokenizer.CharToCode( str[x] ) );
         }
         text.Add( line );
      }

      public bool MakeLineVisible( int lineIndex )
      {
         bool changed = false;
         if ( lineIndex < mScrollBar.Value )
         {
            mScrollBar.Value = Math.Max( 0, lineIndex );
            changed = true;
         }
         else
         {
            int numVisibleLines = GetNumVisibleLines();
            int lastFullyVisible = mScrollBar.Value + numVisibleLines - 2;
            int delta = lineIndex - lastFullyVisible;
            if ( delta > 0 )
            {
               mScrollBar.Value = Math.Max( 0, mScrollBar.Value + delta );
               changed = true;
            }
         }
         return changed;
      }

      public Graphics Paint()
      {
         int numVisibleLines = GetNumVisibleLines();

         int mx = 0;
         int scroll = mScrollBar.Value;
         for ( int i = scroll; i < scroll + numVisibleLines && i < text.Count; ++i )
         {
            mx = Math.Max( mx, text[i].Count );
         }
         int width = Math.Min( 9 * mx + 1 + mMargin, GetVisibleWidth() );
         width = Math.Max( 1, width ); // ensure it doesn't crash when minimized
         b = new Bitmap( width, 9 * numVisibleLines + 1 );

         Graphics g = Graphics.FromImage( b );
         g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
         g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

         int y = 0;
         for ( int i = scroll; i < scroll + numVisibleLines && i < text.Count; ++i )
         {
            List<int> line = text[i];

            int x = 0;
            foreach ( int code in line )
            {
               g.DrawImageUnscaled( VicFont.sChars[code], 9 * x + 1 + mMargin, 9 * y + 1 );
               g.DrawImageUnscaled( VicFont.sOutline, 9 * x + mMargin, 9 * y );
               ++x;
            }
            ++y;
         }

         return g;
      }

      public void Flip()
      {
         if ( b != null )
         {
            if ( mZoom > 1 )
            {
               Bitmap b2 = new Bitmap( mZoom * b.Width, mZoom * b.Height );
               Graphics g = Graphics.FromImage( b2 );
               g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
               g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
               g.DrawImage( b, 0, 0, mZoom * b.Width, mZoom * b.Height );

               mPictureBox.Image = b2;
            }
            else
            {
               mPictureBox.Image = b;
            }
            b = null;
         }
      }

      public void UpdateScrollBar()
      {
         int numVisibleLines = GetNumVisibleLines();

         mScrollBar.Minimum = 0;
         mScrollBar.Maximum = text.Count;
         mScrollBar.SmallChange = 1;
         mScrollBar.LargeChange = numVisibleLines;

         if ( mScrollBar.Visible && numVisibleLines >= text.Count )
         {
            if ( mScrollBar.Value > 0 )
            {
               mScrollBar.Value = 0;
            }
            mScrollBar.Visible = false;
         }
         else if ( !mScrollBar.Visible && numVisibleLines < text.Count )
         {
            mScrollBar.Visible = true;
         }
      }

      public int GetNumVisibleLines()
      {
         int numVisibleLines = ( mPictureBox.Height + 8 * mZoom ) / ( 9 * mZoom );
         return numVisibleLines;
      }

      public int GetVisibleWidth()
      {
         return ( mPictureBox.Width + mZoom - 1 ) / mZoom;
      }
   }
}
