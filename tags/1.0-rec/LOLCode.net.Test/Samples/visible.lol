-->begin code
HAI 
	BTW VISIBLE can be used to print strings
	VISIBLE "HELLO"
	VISIBLE "WORLD"

    BTW Putting an '!' at the end stops it from printing a newline at the end
	VISIBLE "HELLO "!
	VISIBLE "WORLD"
	
	BTW VISIBLE can also be used to print ints
	VISIBLE 123
	VISIBLE 1!
	VISIBLE 2!
	VISIBLE 3
	
	I HAS A VAR3. LOL VAR3 R 10
	I HAS A VAR4. LOL VAR4 R 20
	
    BTW VISIBLE can also be used to print variables
	VISIBLE VAR3
	VISIBLE VAR4
	VISIBLE VAR3!
	VISIBLE VAR4
	
	I HAS A VAR. LOL VAR R "KITTEH"
	I HAS A VAR2. LOL VAR2 R "STEALIN'"
	
	VISIBLE VAR
	VISIBLE VAR2
	VISIBLE VAR!
	VISIBLE VAR2
	
    BTW VISIBLE can also be used to print expressions
    VISIBLE VAR3 UP VAR4 OVAR 2
KTHXBYE
-->begin baseline
HELLO
WORLD
HELLO WORLD
123
123
10
20
1020
KITTEH
STEALIN'
KITTEHSTEALIN'
20