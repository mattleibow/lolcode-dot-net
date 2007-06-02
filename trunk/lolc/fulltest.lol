HAI
  BTW Module/file inclusion
  CAN HAS STDIO?
  CAN HAS "file.lol"?

  BTW Declarations
  I HAS A COUNTER
  I HAS A MAXIMUM
  I HAS A MINIMUM
  I HAS A LIST
  I HAS A NAME
  
  BTW Input and output
  VISIBLE "What is your name?". GIMMEH NAME
  VISIBLE "Your name is "! VISIBLE NAME! VISIBLE "? What a weird name."
  VISIBLE "Count from what? "!
  GIMMEH WORD MINIMUM
  VISIBLE "Count to what? "!
  GIMMEH WORD MAXIMUM

  BTW As we have no method of casting, we can't use the above.
  BTW So... I'll cheat and make them constants.
  LOL MINIMUM R 1
  LOL MAXIMUM R 10

  BTW Error condition
  IZ MINIMUM BIGR THAN MAXIMUM
    DIAF 1 "I can't count backwards =("
  KTHX

  BTW Assignment
  LOL COUNTER R MINIMUM

  BTW Looping
  IM IN YR LOOP
    VISIBLE COUNTER

    I HAS A DIFFERENCE
    LOL DIFFERENCE R COUNTER NERF MINIMUM

    BTW Another way to write an IF (and else)
    IZ MINIMUM UP DIFFERENCE LIEK MAXIMUM OVAR 2
    YARLY
      VISIBLE "Half-way there!"
    NOWAI
      I HAS A NEXTCOUNTER
      TIEMZD NEXTCOUNTER!!2
      IZ NEXTCOUNTER LIEK MAXIMUM?
      YARLY
        VISIBLE "Half-way there (ish)!"
      KTHX
    KTHX

    BTW Test array indexing.
    BTW   The following is equivalent to: (C-style)
    BTW     list[counter] = counter
    BTW     list[counter] /= 2
    LOL COUNTER IN MAH LIST R COUNTER
    OVARZ COUNTER IN MAH LIST!!2
    BTW   Because the array is on the LEFT SIDE of
    BTW the assignment, the array will expand to fill.
    BTW If you had tried to access list[counter] first,
    BTW this would not have happened, and chances are an
    BTW error would have occured or your program would've
    BTW crashed.

    BTW Self-assignment
    UPZ COUNTER!!

    BTW Break
    IZ COUNTER BIGR THAN MAXIMUM?
      GTFO
    KTHX

  KTHX

  BTW More stuff on the same line.
  I HAS A RATS. LOL RATS R 0
  I HAS A SLIMY. LOL SLIMY R 10

  BTW This is just too much to pass up.
  I HAS A BUKKIT. LOL RATS IN MAH BUKKIT R SLIMY

  BYES 0 "Thanks for playing!"
KTHXBYE
