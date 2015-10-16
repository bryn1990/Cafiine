- SD Cafiine -

- Preparation :
  - Prepare a sd card
  - Retrieve the title id of the game/application in which you want to replace files (ex: Mii Maker EU => 00050010-1004A200)
  - Create a folder in the root of the sd card with the title id without the "-" character (ex: Mii Maker EU => 000500101004A200)
    - Title ID must be in UPPERCASE
  - Put your modified files in this folder, omit the "vol/content" folder
    - ex: if your file is in "/vol/content/sound/test.bfstm"
    - then it should look like this in the sdcard : "000500101004A200/sound/test.bfstm"
  
- How to use :
  - Insert the sdcard ... ... ...
  - Launch the kernel exploit
  - Launch sdcafiine with A or B button, both mode support file replacement
    - A button : sdcafiine is in standalone mode (the server is not required)
    - B button : sdcafiine is in log mode (server must be running and IP set), it logs FS function and allow to know if the sd card has been mounted
  - Launch your game/application

- Restrictions :
  - it only works with files in the content folder of a game/application
  - the game/application needs access to the sd card

    )))
    (((
  +-----+
  | S D |]
  `-----'

Feel free to modify, redistribute, improve, ...