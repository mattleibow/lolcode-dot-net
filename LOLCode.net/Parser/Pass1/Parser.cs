using System.Collections.Generic;
using System.Reflection.Emit;

using System;
using System.CodeDom.Compiler;

namespace notdot.LOLCode.Parser.Pass1 {



internal partial class Parser {
	const int _EOF = 0;
	const int _ident = 1;
	const int _intCon = 2;
	const int _realCon = 3;
	const int _stringCon = 4;
	const int _eos = 5;
	const int maxT = 74;
	const int _comment = 75;
	const int _blockcomment = 76;
	const int _continuation = 77;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		//errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(filename, la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(filename, t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }
				if (la.kind == 75) {
				}
				if (la.kind == 76) {
				}
				if (la.kind == 77) {
				}

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}
	
	bool WeakSeparator (int n, int syFol, int repFol) {
		bool[] s = new bool[maxT+1];
		if (la.kind == n) { Get(); return true; }
		else if (StartOf(repFol)) return false;
		else {
			for (int i=0; i <= maxT; i++) {
				s[i] = set[syFol, i] || set[repFol, i] || set[0, i];
			}
			SynErr(n);
			while (!s[la.kind]) Get();
			return StartOf(syFol);
		}
	}
	
	void LOLCode() {
		Expect(6);
		if (la.kind == 7) {
			Get();
			if (la.kind == 8) {
				Get();
				version = LOLCodeVersion.v1_0; 
			} else if (la.kind == 9) {
				Get();
				version = LOLCodeVersion.IRCSPECZ; 
			} else if (la.kind == 10) {
				Get();
				version = LOLCodeVersion.v1_1; 
			} else if (la.kind == 11) {
				Get();
				version = LOLCodeVersion.v1_2; 
			} else SynErr(75);
		}
		while (la.kind == 5) {
			Get();
		}
		Statements();
		Expect(12);
		while (la.kind == 5) {
			Get();
		}
	}

	void Statements() {
		while (StartOf(1)) {
			if (la.kind == 13) {
				FunctionDeclaration();
			} else if (la.kind == 15) {
				VarDecl();
			} else {
				OtherStatement();
			}
			Expect(5);
			while (la.kind == 5) {
				Get();
			}
		}
	}

	void FunctionDeclaration() {
		int arity = 0; string name; 
		Expect(13);
		Expect(14);
		Expect(15);
		Expect(1);
		name = t.val; 
		if (la.kind == 16) {
			Get();
			Expect(1);
			arity++; 
			while (la.kind == 17) {
				Get();
				Expect(16);
				Expect(1);
				arity++; 
			}
		}
		while (la.kind == 5) {
			Get();
		}
		while (StartOf(2)) {
			OtherStatement();
		}
		Expect(18);
		Expect(19);
		Expect(20);
		Expect(21);
		globals.AddSymbol(new UserFunctionRef(name, arity, false)); 
	}

	void VarDecl() {
		Expect(15);
		Expect(22);
		Expect(23);
		Expect(1);
		if (la.kind == 24) {
			Get();
			OtherStatement();
		}
	}

	void OtherStatement() {
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 2) {
			Get();
		} else if (la.kind == 3) {
			Get();
		} else if (la.kind == 4) {
			Get();
		} else if (StartOf(3)) {
			Keyword();
		} else SynErr(76);
		while (StartOf(2)) {
			if (la.kind == 1) {
				Get();
			} else if (la.kind == 2) {
				Get();
			} else if (la.kind == 3) {
				Get();
			} else if (la.kind == 4) {
				Get();
			} else {
				Keyword();
			}
		}
	}

	void Keyword() {
		switch (la.kind) {
		case 25: {
			Get();
			break;
		}
		case 22: {
			Get();
			break;
		}
		case 26: {
			Get();
			break;
		}
		case 27: {
			Get();
			break;
		}
		case 28: {
			Get();
			break;
		}
		case 29: {
			Get();
			break;
		}
		case 30: {
			Get();
			break;
		}
		case 31: {
			Get();
			break;
		}
		case 32: {
			Get();
			break;
		}
		case 33: {
			Get();
			break;
		}
		case 16: {
			Get();
			break;
		}
		case 34: {
			Get();
			break;
		}
		case 35: {
			Get();
			break;
		}
		case 23: {
			Get();
			break;
		}
		case 24: {
			Get();
			break;
		}
		case 17: {
			Get();
			break;
		}
		case 36: {
			Get();
			break;
		}
		case 37: {
			Get();
			break;
		}
		case 38: {
			Get();
			break;
		}
		case 39: {
			Get();
			break;
		}
		case 40: {
			Get();
			break;
		}
		case 41: {
			Get();
			break;
		}
		case 42: {
			Get();
			break;
		}
		case 43: {
			Get();
			break;
		}
		case 44: {
			Get();
			break;
		}
		case 45: {
			Get();
			break;
		}
		case 46: {
			Get();
			break;
		}
		case 47: {
			Get();
			break;
		}
		case 48: {
			Get();
			break;
		}
		case 49: {
			Get();
			break;
		}
		case 50: {
			Get();
			break;
		}
		case 51: {
			Get();
			break;
		}
		case 52: {
			Get();
			break;
		}
		case 53: {
			Get();
			break;
		}
		case 54: {
			Get();
			break;
		}
		case 55: {
			Get();
			break;
		}
		case 56: {
			Get();
			break;
		}
		case 57: {
			Get();
			break;
		}
		case 58: {
			Get();
			break;
		}
		case 59: {
			Get();
			break;
		}
		case 60: {
			Get();
			break;
		}
		case 61: {
			Get();
			break;
		}
		case 62: {
			Get();
			break;
		}
		case 63: {
			Get();
			break;
		}
		case 64: {
			Get();
			break;
		}
		case 65: {
			Get();
			break;
		}
		case 66: {
			Get();
			break;
		}
		case 67: {
			Get();
			break;
		}
		case 68: {
			Get();
			break;
		}
		case 69: {
			Get();
			break;
		}
		case 70: {
			Get();
			break;
		}
		case 71: {
			Get();
			break;
		}
		case 72: {
			Get();
			break;
		}
		case 73: {
			Get();
			break;
		}
		default: SynErr(77); break;
		}
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		LOLCode();

    Expect(0);
	}
	
	bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,T,x,T, T,T,x,x, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x},
		{x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x}

	};
} // end Parser


public class Errors {
	//public string errMsgFormat = "-- \"{0}\" line {1} col {2}: {3}"; // 0=file, 1=line, 2=column, 3=text

	private CompilerErrorCollection cec;
	
	public Errors(CompilerErrorCollection c) {
		this.cec = c;
	}
  
	public void SynErr (string file, int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "intCon expected"; break;
			case 3: s = "realCon expected"; break;
			case 4: s = "stringCon expected"; break;
			case 5: s = "eos expected"; break;
			case 6: s = "\"HAI\" expected"; break;
			case 7: s = "\"TO\" expected"; break;
			case 8: s = "\"1.0\" expected"; break;
			case 9: s = "\"IRCSPECZ\" expected"; break;
			case 10: s = "\"1.1\" expected"; break;
			case 11: s = "\"1.2\" expected"; break;
			case 12: s = "\"KTHXBYE\" expected"; break;
			case 13: s = "\"HOW\" expected"; break;
			case 14: s = "\"DUZ\" expected"; break;
			case 15: s = "\"I\" expected"; break;
			case 16: s = "\"YR\" expected"; break;
			case 17: s = "\"AN\" expected"; break;
			case 18: s = "\"IF\" expected"; break;
			case 19: s = "\"U\" expected"; break;
			case 20: s = "\"SAY\" expected"; break;
			case 21: s = "\"SO\" expected"; break;
			case 22: s = "\"HAS\" expected"; break;
			case 23: s = "\"A\" expected"; break;
			case 24: s = "\"ITZ\" expected"; break;
			case 25: s = "\"CAN\" expected"; break;
			case 26: s = "\"?\" expected"; break;
			case 27: s = "\"GIMMEH\" expected"; break;
			case 28: s = "\"LINE\" expected"; break;
			case 29: s = "\"WORD\" expected"; break;
			case 30: s = "\"LETTAR\" expected"; break;
			case 31: s = "\"GTFO\" expected"; break;
			case 32: s = "\"ENUF\" expected"; break;
			case 33: s = "\"OV\" expected"; break;
			case 34: s = "\"UR\" expected"; break;
			case 35: s = "\"MOAR\" expected"; break;
			case 36: s = "\"IM\" expected"; break;
			case 37: s = "\"IN\" expected"; break;
			case 38: s = "\"KTHX\" expected"; break;
			case 39: s = "\"OUTTA\" expected"; break;
			case 40: s = "\"UPZ\" expected"; break;
			case 41: s = "\"NERFZ\" expected"; break;
			case 42: s = "\"TIEMZD\" expected"; break;
			case 43: s = "\"OVARZ\" expected"; break;
			case 44: s = "\"!!\" expected"; break;
			case 45: s = "\"IZ\" expected"; break;
			case 46: s = "\"YARLY\" expected"; break;
			case 47: s = "\"MEBBE\" expected"; break;
			case 48: s = "\"NOWAI\" expected"; break;
			case 49: s = "\"WTF\" expected"; break;
			case 50: s = "\"OMG\" expected"; break;
			case 51: s = "\"OMGWTF\" expected"; break;
			case 52: s = "\"BYES\" expected"; break;
			case 53: s = "\"DIAF\" expected"; break;
			case 54: s = "\"VISIBLE\" expected"; break;
			case 55: s = "\"INVISIBLE\" expected"; break;
			case 56: s = "\"!\" expected"; break;
			case 57: s = "\"LOL\" expected"; break;
			case 58: s = "\"R\" expected"; break;
			case 59: s = "\"AND\" expected"; break;
			case 60: s = "\"XOR\" expected"; break;
			case 61: s = "\"OR\" expected"; break;
			case 62: s = "\"NOT\" expected"; break;
			case 63: s = "\"BIGR\" expected"; break;
			case 64: s = "\"THAN\" expected"; break;
			case 65: s = "\"SMALR\" expected"; break;
			case 66: s = "\"LIEK\" expected"; break;
			case 67: s = "\"UP\" expected"; break;
			case 68: s = "\"NERF\" expected"; break;
			case 69: s = "\"TIEMZ\" expected"; break;
			case 70: s = "\"OVAR\" expected"; break;
			case 71: s = "\"NOOB\" expected"; break;
			case 72: s = "\"MAH\" expected"; break;
			case 73: s = "\"OF\" expected"; break;
			case 74: s = "??? expected"; break;
			case 75: s = "invalid LOLCode"; break;
			case 76: s = "invalid OtherStatement"; break;
			case 77: s = "invalid Keyword"; break;

			default: s = "error " + n; break;
		}
		cec.Add(new CompilerError(file, line, col, n.ToString(), s));
	}

	public void SemErr (string file, int line, int col, string s) {
		cec.Add(new CompilerError(file, line, col, "", s));
	}
	
	public void SemErr (string s) {
		cec.Add(new CompilerError(null, 0, 0, "", s));
	}
	
	public void Warning (string file, int line, int col, string s) {
		CompilerError ce = new CompilerError(file, line, col, "", s);
		ce.IsWarning = true;
		cec.Add(ce);
	}
	
	public void Warning(string s) {
		CompilerError ce = new CompilerError(null, 0, 0, "", s);
		ce.IsWarning = true;
		cec.Add(ce);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}

}