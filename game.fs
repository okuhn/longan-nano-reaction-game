\ Little reaction game using a RISC-V based Longan nano board, 4 push buttons and Mecrisp Quintus Forth 
\ written by Oliver Kuhn, licensed under GPLv3
\ available at https://github.com/okuhn/longan-nano-reaction-game

#require portdefinitions.fs
#require longan-rgb.fs
#require ms.fs

0 variable successes
0 variable failures

\ configure PB4 .. PB7 as input pull up
: buttons-init  ( -- )
$44440000 PORTB_CRL bic!
$88880000 PORTB_CRL bis!
$f0 PORTB_ODR bis! 
;

\ determine pushed buttons as 4 bit vector
: fetch-button-state ( -- n )
  PORTB_IDR @ $4 rshift $f and $f xor 
;

: button-show ( n -- )
  hex.
;

\ execute word if a button is pressed
: exec-on-button ( a-addr -- )
  fetch-button-state dup if swap execute else 2drop then
;

\ wait until button is pressed and get button state
: wait-for-button ( -- n )
  $0 begin drop fetch-button-state dup until
;

: wait-for-button-and-exec ( a-addr -- )
  wait-for-button swap execute 100 ms 
;

: test-button ( -- )
  ['] hex. wait-for-button-and-exec
;

: change-led ( n -- flag )
  -leds
  case
    dup $1 and ?of +red true endof
    dup $2 and ?of +green true endof
    dup $4 and ?of +blue true endof
    true ?of false endof
  endcase
;

: change-led-on-button ( -- )
  ['] change-led wait-for-button-and-exec
;

\ switch on led according to pressed button until off button is pressed
: led-ctrl ( -- )
  begin change-led-on-button 0= until
;

\ wait for 0 ,, 4095 seconds and then show random led color
\ 0 = red, 1 = green, 2 = blue
: random-led ( -- n )
  cycles $fff and dup ms 
  3 mod 
  dup 0 = red-f 
  dup 1 = green-f 
  dup 2 = blue-f
;

\ evaluate result
\ 0 wrong answer
\ 1 right answer
\ 2 aborted 
: eval-result ( color buttons -- result )
  case
    \ aborted
    dup $8 and ?of #2 endof
    \ right button
    2dup swap 1 swap lshift and ?of #1 endof
    \ wrong button
    true ?of #0 endof
  endcase
  nip
;

: display-result ( flag -- )
  cr
  if ." Right!" 1 successes +!
  else ." Wrong!" 1 failures +!
  then
  
  \ wait 100 ms to ensure buttons are released and stable
  #100 ms
;

: play-one-round ( -- n )
  random-led
  wait-for-button
  -leds
  eval-result
  dup 2 < if dup display-result then
;

\ calculate success rate in %, round half up
: success-rate ( fail succ -- rate )
  dup 200 * -rot + / 1+ 2 /
;

: show-statistics ( -- )
  failures @ 
  successes @ 
  2dup
  cr ." Your result: successes = " . ." , failures = " . ." , success rate = " success-rate . ." %"
  0 successes !
  0 failures !
;

: play ( -- )
  begin play-one-round 2 = until
  show-statistics
  cr ." Game over"
;

: game-init ( -- )
  longan-rgb-init
  buttons-init
;

game-init
play

