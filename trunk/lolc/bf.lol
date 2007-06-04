HAI
BTW This is a BrainFuck interpreter written in LOLCode
BTW It accepts as input a BF program, followed by a "!", followed  by any input to the BF program.
BTW Since BrainFuck is turing-complete, this proves that LOLCode is too

I HAS A INSTRUCTIONS	BTW Array for BF instructions
I HAS A IPTR			BTW Pointer to first empty element in INSTRUCTIONS
LOL IPTR R 0
I HAS A LOOPZ			BTW Array of loop start/end addresses
I HAS A LOOPSTACKZ		BTW Loop stack for building the above two
I HAS A LSPTR			BTW Pointer to first empty element of LOOPSTACKZ
LOL LSPTR R 0

BTW Read in BF instructions, terminated with "!"
IM IN YR CODE
  GIMMEH LETTAR IPTR IN MAH INSTRUCTIONS
  
  IZ IPTR IN MAH INSTRUCTIONS LIEK "["?
    LOL LSPTR IN MAH LOOPSTACKZ R IPTR
    UPZ LSPTR!!
  KTHX
  
  IZ IPTR IN MAH INSTRUCTIONS LIEK "]"?
    I HAS A STARTPTR
    NERFZ LSPTR!!
    LOL STARTPTR R LSPTR IN MAH LOOPSTACKZ
    LOL STARTPTR IN MAH LOOPZ R IPTR
    LOL IPTR IN MAH LOOPZ R STARTPTR
  KTHX

  IZ IPTR IN MAH INSTRUCTIONS LIEK "!"?
    GTFO
  NOWAI
    UPZ IPTR!!
  KTHX    
KTHX

BTW Variables for BF's tape
I HAS A LTAPE
I HAS A RTAPE
I HAS A LPTR
LOL LPTR R 0
I HAS A RPTR
LOL RPTR R 0
I HAS A CELL
LOL CELL R 0

BTW Reset instruction pointer to start
LOL IPTR R 0

BTW Start interpreting
IM IN YR LOOP
  I HAS A THING
  LOL THING R IPTR IN MAH INSTRUCTIONS
  
  BTW Move tape head right
  IZ THING LIEK ">"?
    LOL LPTR IN MAH LTAPE R CELL
    UPZ LPTR!!
    IZ RPTR LIEK 0?
      LOL CELL R 0
    NOWAI
      NERFZ RPTR!!
      LOL CELL R RPTR IN MAH RTAPE
    KTHX
  KTHX
  
  BTW Move tape head left
  IZ THING LIEK "<"?
    LOL RPTR IN MAH RTAPE R CELL
    UPZ RPTR!!
    IZ LPTR LIEK 0?
      LOL CELL R 0
    NOWAI
      NERFZ LPTR!!
      LOL CELL R LPTR IN MAH LTAPE
    KTHX
  KTHX
  
  BTW Increment
  IZ THING LIEK "+"?
    UPZ CELL!!
  KTHX
  
  BTW Decrement
  IZ THING LIEK "-"?
    NERFZ CELL!!
  KTHX
  
  BTW Output produces numbers instead of ASCII characters
  IZ THING LIEK "."?
    VISIBLE CELL!
    VISIBLE " "!
  KTHX
  
  BTW Input doesn't work because we can't convert characters to integers
  BTW Oh well, it doesn't stop it being turing complete
  
  BTW Start of loop
  IZ THING LIEK "[" AND CELL LIEK 0?
    LOL IPTR R IPTR IN MAH LOOPZ
  KTHX
  
  BTW End of loop
  IZ THING LIEK "]" AND CELL NOT LIEK 0?
    LOL IPTR R IPTR IN MAH LOOPZ
  KTHX
  
  BTW End of program!
  IZ THING LIEK "!"?
	GTFO
  KTHX
  
  UPZ IPTR!!
KTHX
KTHXBYE