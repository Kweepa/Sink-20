using System;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Collections.Generic;

namespace Sink_20
{
   public partial class CallStack : DockContent
   {
      Sink20 mSink;

      ScrollableTextBox textBox;

      public CallStack( Sink20 sink )
      {
         InitializeComponent();

         textBox = new ScrollableTextBox( pictureBox, vScrollBar, margin: 9 );

         mSink = sink;
      }

      public void PaintCallStack()
      {
         textBox.UpdateScrollBar();
         PaintCallStackWithoutScrollbarUpdate();
      }

      public void Clear()
      {
         pictureBox.Image = null;
      }

      void PaintCallStackWithoutScrollbarUpdate()
      {
         textBox.text.Clear();
         int currentLineNumber = mSink.emulator.BASICLineNumber();
         textBox.text.Add( mSink.editor.GetLineByLineNumber( currentLineNumber ) );
         for ( int i = mSink.emulator.mCPU.sp + 1; i < 0x200 - 5; ++i )
         {
            if ( mSink.emulator.mMemory.Peek( i ) == 141 )
            {
               int returnLineNumber = mSink.emulator.mMemory.Peek16( i + 1 );
               List<int> line = mSink.editor.GetLineByLineNumber( returnLineNumber );
               if ( line != null )
               {
                  textBox.text.Add( line );
               }
            }
         }

         using ( Graphics g = textBox.Paint() )
         {
            g.DrawImageUnscaled( ScrollableTextBox.Caret, 0, 1 );
         }
         textBox.Flip();

         // stop referencing the editor lines
         textBox.text.Clear();
      }

      private void vScrollBar_ValueChanged( object sender, EventArgs e )
      {
         PaintCallStackWithoutScrollbarUpdate();
      }
   }
}
