
/*
* Serial Port Monitor *
*
*/
//Setup Output 
int A0_Out = 0;

//Setup message bytes 
byte inputByte_0;
byte inputByte_1; 
byte inputByte_2; 
byte inputByte_3;
byte inputByte_4;

//Setup
void setup() {
  Serial.begin(9600); 
}

//Main Loop 
void loop() {

  //Read Buffer
  if (Serial.available() == 5) {
    //Read buffer
    inputByte_0 = Serial.read(); 
    delay(100);
    inputByte_1 = Serial.read(); 
    delay(100);
    inputByte_2 = Serial.read(); 
    delay(100);
    inputByte_3 = Serial.read(); 
    delay(100);
    inputByte_4 = Serial.read();
  }

  //Check for start of Message 
  if(inputByte_0 == 16) {
    //Detect Command type 
    switch (inputByte_1)
    {
      case 128:
      //Say hello
      Serial.write("HELLO FROM ARDUINO"); clearMessageBytes();
      break;
  
      case 129: //read A0
      A0_Out = map(analogRead(A0), 0, 1023, 0, 255); 
      Serial.write(A0_Out);
      delay(10); 
      break;
     }
  } 
}
  
void clearMessageBytes() { 
  //Clear Message bytes
  inputByte_0 = 0; 
  inputByte_1 = 0; 
  inputByte_2 = 0; 
  inputByte_3 = 0; 
  inputByte_4 = 0;
        
  //Let the PC know we are ready for more data 
  Serial.print("-READY TO RECEIVE");
}
