Cafiine compatible with 5.3.2 fw

Compilation :
- in order to compile cafiine you need to put the cafiine folder in your libwiiu project
- libwiuu/cafiine/client must be compiled separately (cd libwiuu/cafiine/client ; make)
- libwiiu/cafiine/installer must be compiled like the other libwiiu examples (with the build.py script)

Pre-made version :
- in www/cafiine, it is already compiled, just put the folder in your server
 (don't forget to change "window.location = "cafiine/payload532.html"; in index.html if needed)

Note :
In cafiine you need to provide the cafiine_server ip, in this version of cafiine you can change it directly in the application.
If you want to change the default ip, you need to recompile the installer (cafiine/installer/cafiine.c) and change the value of DEFAULT_SERVER_IP.

    )))
    (((
  +-----+
  |     |]
  `-----'