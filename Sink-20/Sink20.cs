using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using System.Drawing;
using System.Reflection;

// todo:
// when debugging, keep focus on the emulator window
// search
// fix mouse selection
// option to keep the display running when the processor is paused
// X toolbar with icons (8x8?) for all the usual stuff (undo redo cut paste save step upload etc)
// fix crash in call stack paint after debug then reset
// make Ctrl-X Ctrl-C Ctrl-V work

namespace Sink_20
{
   public partial class Sink20 : Form
   {
      public Editor editor;
      public Emulator emulator;
      public VirtualKeyboard keyboard;
      public Variables variables;
      public CallStack callStack;
      public MemoryConfiguration memConfig;

      void ResizeButtonImage( ToolStripButton button )
      {
         Bitmap b = new Bitmap( 24, 24 );
         Graphics g = Graphics.FromImage( b );
         g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
         g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
         g.DrawImage( button.Image, 0, 0, 24, 24 );
         button.Image = b;
      }

      public Sink20()
      {
         InitializeComponent();

         if ( File.Exists( "panelLayout.xml" ) )
         {
            dockPanel.LoadFromXml( "panelLayout.xml", DeserializeDockContentDelegate );
         }
         else
         {
            editor = new Editor( this );
            editor.Show( dockPanel );
            emulator = new Emulator( this );
            emulator.Show( dockPanel, DockState.DockTop );
            keyboard = new VirtualKeyboard( this );
            keyboard.Show( dockPanel, DockState.DockBottom );
            variables = new Variables( this );
            variables.Show( dockPanel, DockState.DockTop );
            callStack = new CallStack( this );
            callStack.Show( dockPanel, DockState.DockTop );
            memConfig = new MemoryConfiguration( this );
            memConfig.Show( dockPanel, DockState.Float );
         }

         KeyPress += GotKeyPress;
         KeyDown += GotKeyDown;
         KeyUp += GotKeyUp;

         zoomInToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Plus";
         zoomOutToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Minus";

         // rescale ALL buttons :)
         MemberInfo[] members = GetType().FindMembers(
            MemberTypes.Field,
            BindingFlags.NonPublic | BindingFlags.Instance,
            delegate ( MemberInfo m, object o ) { return ( m as FieldInfo ).FieldType == typeof( ToolStripButton ); },
            null );
         foreach ( MemberInfo member in members )
         {
            ResizeButtonImage( (ToolStripButton) ( member as FieldInfo ).GetValue( this ) );
         }
         toolStripButtonCut.Click += delegate ( object sender, EventArgs e ) { editor.Copy( cut: true ); };
         toolStripButtonCopy.Click += delegate ( object sender, EventArgs e ) { editor.Copy( cut: false ); };
         toolStripButtonPaste.Click += delegate ( object sender, EventArgs e ) { editor.Paste(); };
         toolStripButtonSave.Click += delegate ( object sender, EventArgs e ) { editor.Save(); };
         toolStripButtonUndo.Click += delegate ( object sender, EventArgs e ) { editor.Undo(); };
         toolStripButtonRedo.Click += delegate ( object sender, EventArgs e ) { editor.Redo(); };
         toolStripButtonReset.Click += delegate ( object sender, EventArgs e ) { emulator.mCPU.SoftReset(); };
         toolStripButtonPush.Click += delegate ( object sender, EventArgs e ) { TransferProgram(); };
         toolStripButtonPause.Click += delegate ( object sender, EventArgs e ) { Pause(); };
         toolStripButtonPlay.Click += delegate ( object sender, EventArgs e ) { Play(); };
         toolStripButtonStop.Click += delegate ( object sender, EventArgs e ) { };
         toolStripButtonStepIn.Click += delegate ( object sender, EventArgs e ) { };
         toolStripButtonStepOver.Click += delegate ( object sender, EventArgs e ) { StepOver(); };
         toolStripButtonStepOut.Click += delegate ( object sender, EventArgs e ) { };

         Timer timer = new Timer();
         timer.Interval = 2; // ms
         timer.Tick += UpdateCPU;
         timer.Start();
      }

      IDockContent DeserializeDockContentDelegate( string persistString )
      {
         IDockContent dockContent = null;

         string ns = typeof( Sink20 ).Namespace + ".";
         if ( persistString == ns + nameof( Editor ) )
         {
            editor = new Editor( this );
            return editor;
         }
         if ( persistString == ns + nameof( Emulator ) )
         {
            emulator = new Emulator( this );
            return emulator;
         }
         if ( persistString == ns + nameof( VirtualKeyboard ) )
         {
            keyboard = new VirtualKeyboard( this );
            return keyboard;
         }
         if ( persistString == ns + nameof( Variables ) )
         {
            variables = new Variables( this );
            return variables;
         }
         if ( persistString == ns + nameof( CallStack ) )
         {
            callStack = new CallStack( this );
            return callStack;
         }
         if ( persistString == ns + nameof( MemoryConfiguration ) )
         {
            memConfig = new MemoryConfiguration( this );
            return memConfig;
         }

         return dockContent;
      }

      void UpdateCPU( object sender, EventArgs e )
      {
         // only if paused...
         emulator.Think();

         UpdateFreeMem();
      }

      protected override bool IsInputKey( Keys key )
      {
         if ( ( key & Keys.Control ) > 0 )
         {
            return false;
         }
         return true;
      }

      public void GotKeyPress( object sender, KeyPressEventArgs e )
      {
         int code = -1;
         if ( e.KeyChar >= 'a' && e.KeyChar <= 'z' )
         {
            code = e.KeyChar - 'a' + 1;
         }
         else if ( e.KeyChar >= 'A' && e.KeyChar <= 'Z' )
         {
            code = e.KeyChar;
         }
         else if ( e.KeyChar >= ' ' && e.KeyChar <= '?' )
         {
            code = e.KeyChar;
         }
         else
         {
            switch ( e.KeyChar )
            {
            case '[':
               code = 27;
               break;
            case ']':
               code = 29;
               break;
            case '^':
               code = 30;
               break;
            case '@':
               code = 0;
               break;
            }
         }
         if ( code >= 0 )
         {
            if ( !emulator.mHasFocus )
            {
               editor.GotKeyPress( code );
            }
            e.Handled = true;
         }
      }

      public void GotKeyDown( object sender, KeyEventArgs e )
      {
         if ( !emulator.mHasFocus )
         {
            editor.GotKeyDown( e );
         }
         else
         {
            emulator.GotKeyDown( e );
         }
      }

      public void GotKeyUp( object sender, KeyEventArgs e )
      {
         // always send to the emulator - to ensure it gets the key up events
         // even when focus changes.
         emulator.GotKeyUp( e );
      }

      private void UpdateFreeMem()
      {
         toolStripStatusLabel1.Text = $"Free mem: {2 + emulator.mMemory.Peek16(51) - emulator.mMemory.Peek16(49)}";
      }

      private void TransferProgram()
      {
         emulator.LoadProgram(editor.GetProgram());
         UpdateFreeMem();
      }

      private void saveAsToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.SaveAs();
      }

      private void undoToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Undo();
      }

      private void redoToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Redo();
      }

      private void cutToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Copy( cut: true );
      }

      private void copyToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Copy( cut: false );
      }

      private void pasteToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Paste();
      }

      private void selectAllToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.SelectAll();
      }

      private void findToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Find();
      }

      private void replaceToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Replace();
      }

      private void zoomInToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.ZoomIn();
      }

      private void zoomOutToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.ZoomOut();
      }

      private void menuStrip1_MenuActivate( object sender, EventArgs e )
      {
         undoToolStripMenuItem.Enabled = editor.HasUndo();
         redoToolStripMenuItem.Enabled = editor.HasRedo();
         cutToolStripMenuItem.Enabled = editor.HasSelection();
         copyToolStripMenuItem.Enabled = cutToolStripMenuItem.Enabled;
         pasteToolStripMenuItem.Enabled = editor.HasPaste();
         zoomOutToolStripMenuItem.Enabled = editor.HasZoomOut();
      }

      private void aboutToolStripMenuItem_Click( object sender, EventArgs e )
      {
         MessageBox.Show(
            "Sink-20 is a Vic-20 BASIC editor and emulator by Steve McCrea, based loosely on BASin for the ZX Spectrum.",
            "About Sink-20",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information );
      }

      private void saveToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Save();
      }

      private void openToolStripMenuItem_Click( object sender, EventArgs e )
      {
         editor.Open();
      }

      private void transferToolStripMenuItem_Click( object sender, EventArgs e )
      {
         TransferProgram();
      }

      private void runToolStripMenuItem1_Click( object sender, EventArgs e )
      {
         emulator.PokeCommandIntoKeyboardBuffer( "RUN" );
      }

      private void Pause()
      {
         emulator.DebugPause();
         editor.PaintEditor();
         variables.PaintVariables();
      }

      private void pauseToolStripMenuItem_Click( object sender, EventArgs e )
      {
         Pause();
      }

      private void StepOver()
      {
         emulator.DebugStep();
         variables.PaintVariables();
      }

      private void stepToolStripMenuItem_Click( object sender, EventArgs e )
      {
         StepOver();
      }

      private void Play()
      {
         if ( emulator.IsRunning() )
         {
            emulator.PokeCommandIntoKeyboardBuffer( "RUN" );
         }
         else
         {
            emulator.DebugPlay();
            editor.PaintEditor();
            callStack.Clear();
         }
      }

      private void playToolStripMenuItem_Click( object sender, EventArgs e )
      {
         Play();
      }

      private void Sink20_FormClosing( object sender, FormClosingEventArgs e )
      {
         dockPanel.SaveAsXml( "panelLayout.xml" );
      }

      void ToggleDockContent( DockContent dockContent )
      {
         if ( dockContent.IsHidden )
         {
            dockContent.Show();
         }
         else
         {
            dockContent.Hide();
         }
      }

      private void emulatorToolStripMenuItem_Click( object sender, EventArgs e )
      {
         ToggleDockContent( emulator );
      }

      private void editorToolStripMenuItem_Click( object sender, EventArgs e )
      {
         ToggleDockContent( editor );
      }

      private void keyboardToolStripMenuItem_Click( object sender, EventArgs e )
      {
         ToggleDockContent( keyboard );
      }

      private void variablesToolStripMenuItem_Click( object sender, EventArgs e )
      {
         ToggleDockContent( variables );
      }

      private void callStackToolStripMenuItem_Click( object sender, EventArgs e )
      {
         ToggleDockContent( callStack );
      }

      private void windowToolStripMenuItem_DropDownOpening( object sender, EventArgs e )
      {
         emulatorToolStripMenuItem.Checked = !emulator.IsHidden;
         editorToolStripMenuItem.Checked = !editor.IsHidden;
         keyboardToolStripMenuItem.Checked = !keyboard.IsHidden;
         variablesToolStripMenuItem.Checked = !variables.IsHidden;
         callStackToolStripMenuItem.Checked = !callStack.IsHidden;
      }

      private void startProfilingToolStripMenuItem_Click( object sender, EventArgs e )
      {
         emulator.IsProfiling = true;
      }

      private void stopProfilingToolStripMenuItem_Click( object sender, EventArgs e )
      {
         emulator.IsProfiling = false;
      }

      private void clearProfileDataToolStripMenuItem_Click( object sender, EventArgs e )
      {
         emulator.ClearProfileData();
      }

      private void resetToolStripMenuItem_Click( object sender, EventArgs e )
      {
         emulator.mCPU.SoftReset();
      }

      private void memoryConfigurationToolStripMenuItem_Click( object sender, EventArgs e )
      {
         ToggleDockContent( memConfig );
      }

      private void exitToolStripMenuItem_Click( object sender, EventArgs e )
      {
         Application.Exit();
      }
   }
}
