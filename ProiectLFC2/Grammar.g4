grammar Grammar;

// Parser rules
prog:   stat+ ;
stat:   expr NEWLINE ;
expr:   expr ('*'|'/') expr   # MulDiv
    |   expr ('+'|'-') expr   # AddSub
    |   INT                   # Int
    |   '(' expr ')'          # Parens
    ;

// Lexer rules
INT:    [0-9]+ ;
NEWLINE: '\r'? '\n' ;
WS:     [ \t]+ -> skip ;
