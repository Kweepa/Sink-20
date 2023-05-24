using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Drawing;
using System.Collections.Generic;

namespace Sink_20
{
   public partial class Variables : DockContent
   {
      Sink20 mSink;

      ScrollableTextBox textBox;

      class Variable
      {
         public int[] val = new int[7];
         public string stringVal;
         public bool setBreakpoint;
         public bool hitBreakpoint;
      }

      List<Variable> vars = new List<Variable>();
      bool hasBreakpoint;

      public Variables( Sink20 sink )
      {
         InitializeComponent();

         textBox = new ScrollableTextBox( pictureBox, vScrollBar, margin: 9 );

         mSink = sink;
         Timer timer = new Timer();
         timer.Interval = 1000; // ms
         timer.Tick += Think;
         timer.Start();
      }

      enum EVarType
      {
         Float,
         String,
         Integer,
         Invalid
      }

      EVarType GetVarType( Memory mem, int varAdr )
      {
         int name1 = mem.Peek( varAdr );
         int name2 = mem.Peek( varAdr + 1 );
         int type = 2 * ( name1 >> 7 ) + ( name2 >> 7 );
         switch ( type )
         {
         case 0:
            return EVarType.Float;
         case 1:
            return EVarType.String;
         case 3:
            return EVarType.Integer;
         default:
            return EVarType.Invalid;
         }
      }

      string GetVarName( Memory mem, int varAdr )
      {
         int name1 = mem.Peek( varAdr );
         int name2 = mem.Peek( varAdr + 1 );
         name1 &= 0x7f;
         name2 &= 0x7f;

         string name = "";
         name += (char) name1;
         if ( name2 != 0 )
         {
            name += (char) name2;
         }
         else
         {
            name = " " + name;
         }

         switch ( GetVarType( mem, varAdr ) )
         {
         case EVarType.Float:
            name += " ";
            break;
         case EVarType.String:
            name += "$";
            break;
         case EVarType.Integer:
            name += "%";
            break;
         }

         return name;
      }

      int GetNumVariables()
      {
         Memory mem = mSink.emulator.mMemory;
         int varTab = mem.Peek16( 45 );
         int aryTab = mem.Peek16( 47 );
         return ( aryTab - varTab ) / 7;
      }

      int GetNumArrayVariableLines()
      {
         Memory mem = mSink.emulator.mMemory;
         int aryTab = mem.Peek16( 47 );
         int strEnd = mem.Peek16( 49 );
         int aryAdr = aryTab;
         int num = 0;
         while ( aryAdr != strEnd )
         {
            string name = GetVarName( mem, aryAdr );
            EVarType type = GetVarType( mem, aryAdr );
            int size = mem.Peek16( aryAdr + 2 );
            int dim = mem.Peek( aryAdr + 4 );
            int curNum = 1;
            for ( int d = 0; d < dim; ++d )
            {
               int dimSize = 256 * mem.Peek( aryAdr + 5 + 2 * d ) + mem.Peek( aryAdr + 6 + 2 * d );
               curNum *= dimSize;
            }

            aryAdr += size;
            num += curNum;
         }
         return num;
      }

      int GetVarDisplayLength( Memory mem, int varAdr )
      {
         if ( GetVarType( mem, varAdr ) == EVarType.String )
         {
            return mem.Peek( varAdr + 2 );
         }
         // minimum for other types
         return 16;
      }

      string GetStringValue( Memory mem, int varDataAdr )
      {
         string val = "";
         int len = mem.Peek( varDataAdr );
         int addr = mem.Peek16( varDataAdr + 1 );
         for ( int c = 0; c < len; ++c )
         {
            val += (char) mem.Peek( addr + c );
         }
         return val;
      }

      public void PaintVariables()
      {
         textBox.UpdateScrollBar();
         PaintVariablesWithoutScrollbarUpdate();
      }

      string GetVarValue( Memory mem, EVarType type, int varDataAdr )
      {
         string val = "";
         switch ( type )
         {
         case EVarType.Float:
            {
               // convert...
               double d = 0.0;
               int exp = mem.Peek( varDataAdr );
               if ( exp != 0 )
               {
                  int m1 = mem.Peek( varDataAdr + 1 );
                  int m2 = mem.Peek( varDataAdr + 2 );
                  int m3 = mem.Peek( varDataAdr + 3 );
                  int m4 = mem.Peek( varDataAdr + 4 );
                  d = ( ( ( m4 / 256.0 + m3 ) / 256 + m2 ) / 256 + ( m1 | 128 ) ) / 256;
                  d *= ( m1 > 127 ? -1 : 1 ) * Math.Pow( 2.0, exp - 128 );
               }
               val = d.ToString();
            }
            break;
         case EVarType.String:
            {
               val = GetStringValue( mem, varDataAdr );
            }
            break;
         case EVarType.Integer:
            val = ( 256 * mem.Peek( varDataAdr ) + mem.Peek( varDataAdr + 1 ) ).ToString();
            break;
         }
         return val;
      }

      void RecurseAddArrayBlock( ref int dataAdr, EVarType type, string arrayBlockTitle, int[] dims, int curDim )
      {
         if ( curDim == dims.Length - 1 )
         {
            for ( int i = 0; i < dims[curDim]; ++i )
            {
               Memory mem = mSink.emulator.mMemory;
               string val = GetVarValue( mem, type, dataAdr );
               textBox.AddString( arrayBlockTitle + i + ")=" + val );
               switch ( type )
               {
               case EVarType.Integer:
                  dataAdr += 2;
                  break;
               case EVarType.Float:
                  dataAdr += 5;
                  break;
               case EVarType.String:
                  dataAdr += 3;
                  break;
               }
            }
         }
         else
         {
            for ( int i = 0; i < dims[curDim]; ++i )
            {
               RecurseAddArrayBlock( ref dataAdr, type, arrayBlockTitle + i + ",", dims, curDim + 1 );
            }
         }
      }

      void PaintVariablesWithoutScrollbarUpdate()
      {
         // if paused or flashing the cursor, update variables
         // this is a cheap way to avoid the problem of the variable memory being in flux
         const int CURSOR_DISABLE = 0xcc;
         if ( mSink.emulator.IsRunning() && mSink.emulator.mMemory.Peek( CURSOR_DISABLE ) != 0 )
         {
            return;
         }
         // go grubbing around in BASIC memory
         Memory mem = mSink.emulator.mMemory;
         int varTab = mem.Peek16( 45 );
         int aryTab = mem.Peek16( 47 );
         int strEnd = mem.Peek16( 49 );

         textBox.text.Clear();
         for ( int i = 0; i < GetNumVariables(); ++i )
         {
            int varAdr = varTab + 7 * i;

            if ( vars.Count <= i )
            {
               vars.Add( new Variable() );
            }
            Variable var = vars[i];
            for ( int k = 0; k < 7; ++k )
            {
               var.val[k] = mem.Peek( varAdr + k );
            }

            string name = GetVarName( mem, varAdr );
            EVarType type = GetVarType( mem, varAdr );
            string val = GetVarValue( mem, type, varAdr + 2 );
            if ( type == EVarType.String )
            {
               var.stringVal = val;
            }

            textBox.AddString( name + "=" + val );
         }
         int aryAdr = aryTab;
         while ( aryAdr < strEnd )
         {
            string name = GetVarName( mem, aryAdr );
            EVarType type = GetVarType( mem, aryAdr );
            int size = mem.Peek16( aryAdr + 2 );
            int aryEnd = aryAdr + size;
            int dim = mem.Peek( aryAdr + 4 );
            int[] dims = new int[dim];
            aryAdr += 5;
            int varSize = 1;
            switch ( type )
            {
            case EVarType.Integer:
               varSize = 2;
               break;
            case EVarType.Float:
               varSize = 5;
               break;
            case EVarType.String:
               varSize = 3;
               break;
            }
            for ( int d = 0; d < dim; ++d )
            {
               dims[d] = 256 * mem.Peek( aryAdr ) + mem.Peek( aryAdr + 1 );
               aryAdr += 2;
            }
            int varAdr = aryAdr;
            while ( varAdr < aryEnd )
            {
               int off = varAdr - aryAdr;
               string indices = "";
               int blockSize = aryEnd - aryAdr;
               for ( int i = 0; i <  dims.Length; ++i )
               {
                  blockSize /= dims[i];
                  indices = ( off / blockSize ) + ( indices.Length > 0 ? "," : "" ) + indices;
                  off %= blockSize;
               }
               textBox.AddString( name + "(" + indices + ")=" + GetVarValue( mem, type, varAdr ) );
               varAdr += varSize;
            }
            aryAdr = aryEnd;
         }

         using ( Graphics g = textBox.Paint() )
         {
            int scroll = vScrollBar.Value;
            for ( int i = scroll; i < vars.Count; ++i )
            {
               if ( vars[i].setBreakpoint )
               {
                  g.DrawImage( ScrollableTextBox.Heart, 0, 9 * ( i - scroll ) + 1 );
               }
               if ( vars[i].hitBreakpoint )
               {
                  g.DrawImage( ScrollableTextBox.Highlight, textBox.mMargin + 1, 9 * ( i - scroll ) + 1, 26, 8 );
               }
            }
         }
         textBox.Flip();
      }

      void Think( object sender, EventArgs e )
      {
         PaintVariables();
      }

      private void vScrollBar_ValueChanged( object sender, EventArgs e )
      {
         PaintVariablesWithoutScrollbarUpdate();
      }

      private void pictureBox_MouseDown( object sender, MouseEventArgs e )
      {
         if ( e.Button == MouseButtons.Left )
         {
            int i = e.Y / ( textBox.mZoom * 9 ) + vScrollBar.Value;
            if ( i < vars.Count )
            {
               vars[i].setBreakpoint = !vars[i].setBreakpoint;
               PaintVariables();
            }
         }
         hasBreakpoint = false;
         for ( int i = 0; i < vars.Count; ++i )
         {
            if ( vars[i].setBreakpoint )
            {
               hasBreakpoint = true;
               break;
            }
         }
      }

      public bool VariableChanged()
      {
         bool changed = false;
         if ( hasBreakpoint )
         {
            Memory mem = mSink.emulator.mMemory;
            int varTab = mem.Peek16( 45 );

            for ( int i = 0; i < vars.Count; ++i )
            {
               int varAdr = varTab + 7 * i;
               Variable var = vars[i];
               if ( var.setBreakpoint )
               {
                  for ( int k = 2; k < 7; ++k )
                  {
                     if ( mem.Peek( varAdr + k ) != var.val[k] )
                     {
                        changed = true;
                        break;
                     }
                  }
                  if ( !changed
                     && GetVarType( mem, varAdr ) == EVarType.String
                     && var.stringVal != GetStringValue( mem, varAdr ) )
                  {
                     changed = true;
                  }
                  if ( changed )
                  {
                     var.hitBreakpoint = true;
                     break;
                  }
               }
            }
         }
         return changed;
      }
   }
}
