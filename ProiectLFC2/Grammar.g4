grammar Grammar;

// --- SECTIUNEA 1: PARSER RULES (Persoana 2: va extinde statements si expressions) ---

program
    : (declaration)* EOF
    ;

declaration
    : functionDefinition 
    | globalDeclaration 
    ;

globalDeclaration
    : (CONST)? type ID ('=' initialValue)? ';' 
    ;

functionDefinition
    : returnType ID LPAREN (parameterList)? RPAREN block 
    ;

parameterList
    : parameter (COMMA parameter)*
    ;

parameter
    : type ID
    ;

returnType
    : type | VOID
    ;

type // Regula de Parser: Face referire la token-urile de tip (INT, FLOAT, etc.)
    : INT | FLOAT | DOUBLE | STRING
    ;

block
    : LBRACE (statement)* RBRACE
    ;

statement
    : declarationStatement ';'
    | assignmentStatement ';'
    | ifStatement
    | forStatement
    | whileStatement
    | returnStatement ';'
    | callStatement ';'
    | block 
    | ';' // Statement vid
    ;

declarationStatement
    : (CONST)? type ID ('=' expression)? 
    ;

assignmentStatement
    : ID (ASSIGN | PLUS_ASSIGN | MINUS_ASSIGN | MUL_ASSIGN | DIV_ASSIGN | MOD_ASSIGN) expression 
    | ID INC 
    | ID DEC 
    ;

ifStatement
    : IF LPAREN expression RPAREN statement (ELSE statement)? 
    ;

forStatement
    : FOR LPAREN (declarationStatement | assignmentStatement | )? SEMICOLON expression? SEMICOLON expression? RPAREN statement 
    ;

whileStatement
    : WHILE LPAREN expression RPAREN statement 
    ;

returnStatement
    : RETURN expression? 
    ;

callStatement
    : ID LPAREN (expressionList)? RPAREN
    ;

expressionList
    : expression (COMMA expression)*
    ;

expression
    : expression (OR) expression          # LogicalOr
    | expression (AND) expression         # LogicalAnd
    | expression (EQ | NEQ) expression    # Equality
    | expression (LT | GT | LE | GE) expression # Relational
    | expression (PLUS | MINUS) expression # AddSub
    | expression (MUL | DIV | MOD) expression # MulDivMod
    | unaryExpression                     # Unary
    ;

unaryExpression
    : (PLUS | MINUS | NOT)? primary         # UnaryOperator
    | INC primary                           # PreIncrement
    | DEC primary                           # PreDecrement
    | primary (INC | DEC)                   # PostIncrementDecrement
    ;

primary
    : ID
    | initialValue
    | LPAREN expression RPAREN
    | callStatement
    ;

initialValue
    : NUMBER 
    | STRING_LITERAL 
    ;


// --- SECTIUNEA 2: LEXER RULES (Persoana 1) ---

// Cuvinte cheie 
INT     : 'int';
FLOAT   : 'float';
DOUBLE  : 'double';
STRING  : 'string';
CONST   : 'const';
VOID    : 'void';
IF      : 'if';
ELSE    : 'else';
FOR     : 'for';
WHILE   : 'while';
RETURN  : 'return';

// Operatori Aritmetici 
PLUS    : '+';
MINUS   : '-';
MUL     : '*';
DIV     : '/';
MOD     : '%';

// Operatori Relaționali 
LE      : '<=';
GE      : '>=';
EQ      : '==';
NEQ     : '!=';
LT      : '<';
GT      : '>';

// Operatori Logici 
AND     : '&&';
OR      : '||';
NOT     : '!';

// Operatori de Atribuire 
PLUS_ASSIGN : '+=';
MINUS_ASSIGN : '-=';
MUL_ASSIGN : '*=';
DIV_ASSIGN : '/=';
MOD_ASSIGN : '%=';
ASSIGN  : '=';

// Operatori de Incrementare/Decrementare 
INC     : '++';
DEC     : '--';

// Delimitatori 
LPAREN  : '(';
RPAREN  : ')';
LBRACE  : '{';
RBRACE  : '}';
COMMA   : ',';
SEMICOLON : ';';

// Identificatori 
ID
    : LETTER (LETTER | DIGIT | '_')*
    ;

// Constante numerice 
NUMBER
    : DIGIT+ ('.' DIGIT+)? (('e' | 'E') ('+' | '-')? DIGIT+)?
    ;

// Literali (Șiruri de caractere) 
STRING_LITERAL
    : '"' ( ~["\r\n] )* '"' 
    ;

// --- GESTIONAREA ERORILOR LEXICALE SPECIFICE ---

// Eroare: Șir de caractere neînchis și pe mai multe rânduri 
UNTERMINATED_STRING 
    : '"' ( ~["] )* ( '\n' | '\r' | EOF ) -> type(ERROR_TOKEN)
    ;
    
// Spații albe și comentarii (Neglijarea lor)
WHITESPACE : [ \t\n\r]+ -> skip;

// Comentarii de tip linie 
LINE_COMMENT
    : '//' .*? ('\n' | '\r' | EOF) -> skip
    ;

// Comentarii de tip bloc 
BLOCK_COMMENT
    : '/*' .*? '*/' -> skip
    ;
    
// Fragment de ajutor
fragment LETTER : [a-zA-Z];
fragment DIGIT : [0-9];

// Eroare: Caractere care nu fac parte din vocabular 
ERROR_TOKEN 
    : . 
    ;