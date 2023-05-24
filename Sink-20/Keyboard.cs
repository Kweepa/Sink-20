namespace Sink_20
{
   public class Keyboard
   {
      /*
      VIC-20 KEYBOARD MATRIX
              
      9121   9120

             0       1       2       3       4       5       6       7

       0     1       Lft_arr Control RunStop Space   CBM     q       2
       1     3       w       a       Shift_L z       s       e       4
       2     5       r       d       x       c       f       t       6
       3     7       y       g       v       b       h       u       8
       4     9       i       j       n       m       k       o       0
       5     +       p       l       ","     .       :       @       -
       6     GBP     *       ;       /       Shift_R =       Up_arr  Home
       7     Del     Return  Right   Down    F1      F3      F5      F7

       */
      
      int[] keyBind = new int[64]
      {
         '1', 192, 20, 9, ' ', 17, 'Q', '2',
         '3', 'W', 'A', 16, 'Z', 'S', 'E', '4',
         '5', 'R', 'D', 'X', 'C', 'F', 'T', '6',
         '7', 'Y', 'G', 'V', 'B', 'H', 'U', '8',
         '9', 'I', 'J', 'N', 'M', 'K', 'O', '0',
         189, 'P', 'L', 188, 190, 186, 219, 187,
         8, 221, 222, 191, 16, 34, 220, 36,
         46, 13, 39, 40, 112, 114, 116, 118
      };
      int[] keyMap = new int[256];
      int[] keyMatrix = new int[8];

      public Keyboard()
      {
         for (int i = 0; i < keyMap.Length; ++i) keyMap[i] = -1;
         for (int i = 0; i < keyBind.Length; ++i) keyMap[keyBind[i]] = i;
         for (int i = 0; i < keyMatrix.Length; ++i) keyMatrix[i] = 255;
      }

      public void KeyDown(int k, bool v)
      {
         //System.Console.WriteLine("Key {0}", k);
         int rc = keyMap[k];
         if (rc >= 0)
         {
            int row = rc >> 3;
            int col = rc & 7;
            keyMatrix[col] &= (255 - (1 << row));
            keyMatrix[col] |= ((v ? 0 : 1) << row);
         }
      }

      public int readStick(int port, int dataDirection)
      {
         int val = port;
         val &= dataDirection;
         val |= (255 - dataDirection);
         return val;
      }

      public int read(int port, int dataDirection)
      {
         int val = 0xff;
         for (int m = 1, i = 0; i < 8; m <<= 1, ++i)
         {
            val &= (port & m) == 0 ? keyMatrix[i] : 255;
         }
         return val & (255 - dataDirection);
      }
   }
}
