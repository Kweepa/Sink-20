using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Sink_20
{
   public partial class Editor : DockContent
   {
      ScrollableTextBox textBox;
      int cx, cy;
      Sink20 mSink;
      Bitmap cursor;
      Bitmap selectBox;

      int sx, sy, ex, ey;
      List<List<int>> pasteBuffer;

      string mFileName = "";

      enum EUndoType
      {
         Combinable,
         Unique
      }

      class UndoItem
      {
         public List<List<int>> text;
         public int cx, cy;
         public EUndoType undoType;
         public int scroll;
      }

      List<UndoItem> undoStack = new List<UndoItem>();
      int undoStackPos = 0;
      EUndoType typingUndoType;

      List<List<int>> DeepCopy( List<List<int>> src )
      {
         List<List<int>> dst = new List<List<int>>();
         foreach ( List<int> line in src )
         {
            dst.Add( new List<int>( line ) );
         }
         return dst;
      }

      void PopUndo()
      {
         int pos = undoStackPos - 1;
         textBox.text = DeepCopy( undoStack[pos].text );
         cx = sx = ex = undoStack[pos].cx;
         cy = sy = ey = undoStack[pos].cy;
         vScrollBar.Value = undoStack[pos].scroll;
         PaintEditor();

         typingUndoType = EUndoType.Unique;
      }

      void PushUndo( EUndoType undoType )
      {
         if ( undoType == EUndoType.Combinable
            && undoStack[undoStackPos - 1].undoType == EUndoType.Combinable )
         {
            // just replace what's there
            --undoStackPos;
         }
         if ( undoStackPos == undoStack.Count )
         {
            undoStack.Add( new UndoItem() );
         }
         undoStack[undoStackPos].text = DeepCopy( textBox.text );
         undoStack[undoStackPos].cx = cx;
         undoStack[undoStackPos].cy = cy;
         undoStack[undoStackPos].undoType = undoType;
         undoStack[undoStackPos].scroll = vScrollBar.Value;
         ++undoStackPos;
         undoStack.RemoveRange( undoStackPos, undoStack.Count - undoStackPos );

         typingUndoType = EUndoType.Unique;
      }

      public bool HasUndo()
      {
         return undoStackPos > 1;
      }

      public void Undo()
      {
         if ( HasUndo() )
         {
            --undoStackPos;
            PopUndo();
         }
      }

      public bool HasRedo()
      {
         return undoStackPos < undoStack.Count;
      }

      public void Redo()
      {
         if ( HasRedo() )
         {
            ++undoStackPos;
            PopUndo();
         }
      }

      public Editor( Sink20 _sink )
      {
         InitializeComponent();

         mSink = _sink;

         cursor = new Bitmap( 1, 1 );
         cursor.SetPixel( 0, 0, Color.Red );

         selectBox = new Bitmap( 1, 1 );
         selectBox.SetPixel( 0, 0, Color.FromArgb( 0x3f0000ff ) );

         textBox = new ScrollableTextBox( pictureBox, vScrollBar, margin: 18 );

         textBox.text.Add( new List<int>() );
         PushUndo( EUndoType.Unique );
         typingUndoType = EUndoType.Combinable;

         PaintEditor();

         pictureBox.MouseDown += pictureBox_MouseDown;
      }

      public void GotKeyPress( int code )
      {
         textBox.text[cy].Insert( cx, code );
         ++cx;
         sx = ex = cx;
         sy = ey = cy;
         PaintEditor();
         PushUndo( typingUndoType );
         typingUndoType = EUndoType.Combinable;
      }

      protected override bool IsInputKey(Keys keyData)
      {
         switch (keyData)
         {
            case Keys.Left:
            case Keys.Right:
            case Keys.Up:
            case Keys.Down:
            case Keys.Shift | Keys.Left:
            case Keys.Shift | Keys.Right:
            case Keys.Shift | Keys.Up:
            case Keys.Shift | Keys.Down:
               return true;
         }
         return base.IsInputKey(keyData);
      }

      public void GotKeyDown( KeyEventArgs e )
      {
         bool refresh = false;

         bool typing = true;

         List<List<int>> text = textBox.text;

         switch ( e.KeyCode )
         {
         case Keys.Up:
            e.Handled = true;
            if ( cy > 0 )
            {
               --cy;
               cx = Math.Min( cx, text[cy].Count );
               refresh = true;
            }
            break;
         case Keys.Down:
            e.Handled = true;
            if ( cy < text.Count - 1 )
            {
               ++cy;
               cx = Math.Min( cx, text[cy].Count );
               refresh = true;
            }
            break;
         case Keys.PageUp:
            e.Handled = true;
            {
               int jump = textBox.GetNumVisibleLines() - 1;
               if ( jump > 0 && cy > 0 )
               {
                  cy = Math.Max( 0, cy - jump );
                  cx = Math.Min( cx, text[cy].Count );
                  refresh = true;
               }
            }
            break;
         case Keys.PageDown:
            e.Handled = true;
            {
               int jump = textBox.GetNumVisibleLines() - 1;
               if ( jump > 0 && cy < text.Count - 1 )
               {
                  cy = Math.Min( cy + jump, text.Count - 1 );
                  cx = Math.Min( cx, text[cy].Count );
                  refresh = true;
               }
            }
            break;
         case Keys.Left:
            e.Handled = true;
            if ( cx > 0 )
            {
               --cx;
               refresh = true;
            }
            else if ( cy > 0 )
            {
               --cy;
               cx = text[cy].Count;
               refresh = true;
            }
            break;
         case Keys.Right:
            e.Handled = true;
            if ( cx < text[cy].Count )
            {
               ++cx;
               refresh = true;
            }
            else if ( cy < text.Count - 1 )
            {
               ++cy;
               cx = 0;
               refresh = true;
            }
            break;
         case Keys.Enter:
            e.Handled = true;
            {
               InsertCR();
               refresh = true;
            }
            break;
         case Keys.Back:
            e.Handled = true;
            if ( cx > 0 )
            {
               --cx;
               text[cy].RemoveAt( cx );
               refresh = true;
            }
            else if ( cy > 0 )
            {
               --cy;
               cx = text[cy].Count;
               text[cy].AddRange( text[cy + 1] );
               text.RemoveAt( cy + 1 );
               refresh = true;
            }
            break;
         case Keys.Delete:
            e.Handled = true;
            if ( cx < text[cy].Count )
            {
               text[cy].RemoveAt( cx );
               refresh = true;
            }
            else if ( cy < text.Count - 1 )
            {
               text[cy].AddRange( text[cy + 1] );
               text.RemoveAt( cy + 1 );
               refresh = true;
            }
            break;
         case Keys.Home:
            e.Handled = true;
            if ( cx > 0 )
            {
               cx = 0;
               refresh = true;
            }
            break;
         case Keys.End:
            e.Handled = true;
            if ( cx < text[cy].Count )
            {
               cx = text[cy].Count;
               refresh = true;
            }
            break;
         }

         if ( refresh )
         {
            if ( ( ModifierKeys & Keys.Shift ) > 0 )
            {
               if ( cy < sy )
               {
                  sy = cy;
                  sx = cx;
               }
               else if ( cy == sy && cx < sx )
               {
                  sx = cx;
               }

               if ( cy > ey )
               {
                  ey = cy;
                  ex = cx;
               }
               else if ( cy == ey && cx > ex )
               {
                  ex = cx;
               }
            }
            else
            {
               sx = ex = cx;
               sy = ey = cy;
            }
            textBox.MakeLineVisible( cy );
            PaintEditor();
            PushUndo( typing ? typingUndoType : EUndoType.Unique );
         }
         if ( typing )
         {
            typingUndoType = EUndoType.Combinable;
         }
      }


      public void Break( int lineNumber )
      {
         int lineIndex;
         if ( GetLineIndexByLineNumber( out lineIndex, lineNumber ) )
         {
            textBox.MakeLineVisible( lineIndex );
         }
      }

      void InsertCR()
      {
         List<List<int>> text = textBox.text;

         int len = text[cy].Count;
         if ( cx < len )
         {
            text.Insert( cy + 1, text[cy].GetRange( cx, len - cx ) );
            text[cy] = text[cy].GetRange( 0, cx );
         }
         else
         {
            text.Insert( cy + 1, new List<int>() );
         }
         sy = ey = ++cy;
         cx = sx = ex = 0;
      }

      public bool HasSelection()
      {
         return sx != ex || sy != ey;
      }

      public bool HasPaste()
      {
         return pasteBuffer != null && pasteBuffer.Count > 0;
      }

      public void Copy( bool cut )
      {
         if ( HasSelection() )
         {
            List<List<int>> text = textBox.text;

            pasteBuffer = new List<List<int>>();
            if ( sy == ey )
            {
               pasteBuffer.Add( text[sy].GetRange( sx, ex - sx ) );
               if ( cut )
               {
                  text[sy].RemoveRange( sx, ex - sx );
               }
            }
            else
            {
               pasteBuffer.Add( text[ey].GetRange( 0, ex ) );
               if ( ex == text[ey].Count )
               {
                  pasteBuffer.Add( new List<int>() );
               }
               if ( cut )
               {
                  text[ey].RemoveRange( 0, ex );
                  if ( text[ey].Count == 0 )
                  {
                     text.RemoveAt( ey );
                  }
               }
               for ( int y = ey - 1; y > sy; --y )
               {
                  pasteBuffer.Insert( 0, new List<int>( text[y] ) );
                  if ( cut )
                  {
                     text.RemoveAt( y );
                  }
               }
               pasteBuffer.Insert( 0, text[sy].GetRange( sx, text[sy].Count - sx ) );
               text[sy].RemoveRange( sx, text[sy].Count - sx );
               if ( sx == 0 && sy < text.Count - 1 )
               {
                  text.RemoveAt( sy );
               }
            }
            if ( cut )
            {
               cx = ex = sx;
               cy = ey = sy;

               PaintEditor();
               PushUndo( EUndoType.Unique );
            }
         }
      }

      public void Paste()
      {
         if ( HasPaste() )
         {
            for ( int i = 0; i < pasteBuffer.Count; ++i )
            {
               textBox.text[cy].InsertRange( cx, pasteBuffer[i] );
               cx += pasteBuffer[i].Count;
               if ( i != pasteBuffer.Count - 1 )
               {
                  InsertCR();
               }
            }
            sy = ey = cy;
            sx = ex = cx;
            PaintEditor();
            PushUndo( EUndoType.Unique );
         }
      }

      public void SelectAll()
      {
         sx = 0; sy = 0;
         ey = textBox.text.Count - 1;
         ex = textBox.text[textBox.text.Count - 1].Count;
         PaintEditor();
      }

      public void Find()
      {

      }

      public void Replace()
      {

      }

      public void ZoomIn()
      {
         ++textBox.mZoom;
         PaintEditor();
      }

      public bool HasZoomOut()
      {
         return textBox.mZoom > 1;
      }

      private void vScrollBar_ValueChanged( object sender, EventArgs e )
      {
         PaintEditorWithoutScrollbarUpdate();
      }

      public void ZoomOut()
      {
         if ( HasZoomOut() )
         {
            --textBox.mZoom;
            PaintEditor();
         }
      }

      private void pictureBox_SizeChanged( object sender, EventArgs e )
      {
         PaintEditor();
      }

      public byte[] GetProgram()
      {
         return Tokenizer.Tokenize( textBox.text, mSink.emulator.mMemory.GetLoadAddress() );
      }

      public void Save()
      {
         if (!string.IsNullOrEmpty(mFileName))
         {
            System.IO.File.WriteAllBytes(mFileName, GetProgram());
         }
      }

      public void SaveAs()
      {
         SaveFileDialog d = new SaveFileDialog();
         d.Filter = "PRG files (*.prg)|*.prg";
         if ( d.ShowDialog() == DialogResult.OK )
         {
            mFileName = d.FileName;
            Save();
         }
      }

      private void pictureBox_MouseDown( object sender, MouseEventArgs e )
      {
         List<List<int>> text = textBox.text;

         int y = e.Y / ( 9 * textBox.mZoom ) + vScrollBar.Value;
         if ( e.X >= textBox.mMargin && e.Button == MouseButtons.Left )
         {
            y = Math.Min( y, text.Count - 1 );
            int x = ( e.X - textBox.mMargin ) / ( 9 * textBox.mZoom );
            x = Math.Min( x, text[y].Count );
            cx = sx = ex = x;
            cy = sy = ey = y;
            PaintEditor();
         }
         else
         {
            if ( y < text.Count && e.Button == MouseButtons.Left )
            {
               int lineNumber;
               int indexAfterLineNumber;
               if ( Tokenizer.GetLineNumber( out lineNumber, out indexAfterLineNumber, text[y] ) )
               {
                  mSink.emulator.ToggleBreakpoint( lineNumber );
                  PaintEditor();
               }
            }
         }
         Activate();
      }

      public void Open()
      {
         OpenFileDialog d = new OpenFileDialog();
         d.Filter = "PRG files (*.prg)|*.prg";
         if ( d.ShowDialog() == DialogResult.OK )
         {
            mFileName = d.FileName;
            byte[] b = System.IO.File.ReadAllBytes( mFileName );
            textBox.text = Tokenizer.Detokenize( b );
            cx = sx = ex = 0;
            cy = sy = ey = 0;
            vScrollBar.Value = 0;
            PaintEditor();
         }
      }

      public List<int> GetLineByLineNumber( int desiredLineNumber )
      {
         int lineIndex;
         if ( GetLineIndexByLineNumber( out lineIndex, desiredLineNumber ) )
         {
            return textBox.text[lineIndex];
         }
         return null;
      }

      public bool GetLineIndexByLineNumber( out int lineIndex, int desiredLineNumber )
      {
         for ( int i = 0; i < textBox.text.Count; ++i )
         {
            List<int> line = textBox.text[i];
            int lineNumber;
            int indexAfterLineNumber;
            if ( Tokenizer.GetLineNumber( out lineNumber, out indexAfterLineNumber, line ) )
            {
               if ( lineNumber == desiredLineNumber )
               {
                  lineIndex = i;
                  return true;
               }
            }
         }
         lineIndex = 0;
         return false;
      }

      void PaintEditorWithoutScrollbarUpdate()
      {
         Dictionary<int, long> profileData = mSink.emulator != null ? mSink.emulator.GetProfileData() : null;
         bool hasProfileData = profileData != null && profileData.Count > 0;
         int numVisibleLines = textBox.GetNumVisibleLines();
         int scroll = vScrollBar.Value;
         int margin = textBox.mMargin;

         List<List<int>> text = textBox.text;

         using ( Graphics g = textBox.Paint() )
         {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            for ( int i = sy; i <= ey && i < scroll + numVisibleLines; ++i )
            {
               List<int> line = text[i];
               for ( int x = ( i == sy ? sx : 0 ); x < ( i == ey ? ex : line.Count ); ++x )
               {
                  g.DrawImage( selectBox, 9 * x + 1 + margin, 9 * ( i - scroll ) + 1, 8, 8 );
               }
               bool quoted = false;
               int quotedStringLength = 0;
               for ( int x = 0; x < line.Count; ++x )
               {
                  if ( line[x] == '\"' )
                  {
                     quoted = !quoted;
                     if ( quoted )
                     {
                        quotedStringLength = 0;
                     }
                  }
                  else if ( quoted )
                  {
                     g.DrawImage( ScrollableTextBox.PurestGreen, 9 * x + margin, 9 * ( i - scroll + 1 ), 9, 1 );
                     // only increment if not a control code (colour, cursor movement, etc)
                     ++quotedStringLength;
                     if ( ( quotedStringLength % 22 ) == 0 )
                     {
                        g.DrawImage( ScrollableTextBox.PurestGreen, 9 * x + 1 + margin, 9 * ( i - scroll ), 9, 9 );
                     }
                     else if ( ( quotedStringLength % 11 ) == 0 )
                     {
                        g.DrawImage( ScrollableTextBox.PurestGreen, 9 * x + 1 + margin, 9 * ( i - scroll ) + 5, 9, 4 );
                     }
                  }
               }
            }

            if ( mSink.emulator != null )
            {
               for ( int i = scroll; i < scroll + numVisibleLines && i < text.Count; ++i )
               {
                  List<int> line = text[i];
                  int executingLineNumber = mSink.emulator.BASICLineNumber();
                  int lineNumber = 0;
                  int indexAfterLineNumber = 0;
                  if ( Tokenizer.GetLineNumber( out lineNumber, out indexAfterLineNumber, line ) )
                  {
                     int y = i - scroll;
                     if ( executingLineNumber == lineNumber )
                     {
                        g.DrawImage( ScrollableTextBox.Caret, 9, 9 * y + 1, 8, 8 );
                     }
                     if ( mSink.emulator.HasBreakpoint( lineNumber ) )
                     {
                        g.DrawImageUnscaled( ScrollableTextBox.Heart, 0, 9 * y + 1 );
                     }
                     long cycles;
                     if ( profileData.TryGetValue( lineNumber, out cycles ) && cycles > 0 )
                     {
                        // each character block is one second
                        g.DrawImage( ScrollableTextBox.PurestGreen, 9, 9 * y + 1, 9 * cycles / VIC.CLOCK_FREQUENCY, 8 );
                     }
                  }
               }
            }
            g.DrawImage( cursor, 9 * cx + margin, 9 * ( cy - scroll ), 1, 10 );
         }

         textBox.Flip();
      }

      public void PaintEditor()
      {
         textBox.UpdateScrollBar();
         PaintEditorWithoutScrollbarUpdate();
      }
   }
}
