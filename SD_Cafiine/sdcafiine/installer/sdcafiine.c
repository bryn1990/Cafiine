#include "../../libwiiu/src/coreinit.h"
#include "../../libwiiu/src/socket.h"
#include "../../libwiiu/src/vpad.h"

/* misc */
typedef unsigned int uint;

#if VER == 532
    #include "fs532.h"

    /* Function addresses for 5.3.2 */
    #define memcpy ((void * (*)(void * dest, const void * src, int num))0x1035a6c)
    #define DCFlushRange ((void (*)(const void*, int))0x01023ee8)
    #define _Exit ((void (*)(void))0x0101cd70)
    #define OSEffectiveToPhysical ((void* (*)(const void*))0x0101f510)

    #define OSScreenInit ((void (*)())0x0103a880)
    #define OSScreenGetBufferSizeEx ((unsigned int (*)(unsigned int bufferNum))0x0103a91c)
    #define OSScreenSetBufferEx ((unsigned int (*)(unsigned int bufferNum, void * addr))0x0103a934)
    #define OSScreenClearBufferEx ((unsigned int (*)(unsigned int bufferNum, unsigned int temp))0x0103aa90)
    #define OSScreenFlipBuffersEx ((unsigned int (*)(unsigned int bufferNum))0x0103a9d0)
    #define OSScreenPutFontEx ((unsigned int (*)(unsigned int bufferNum, unsigned int posX, unsigned int posY, void * buffer))0x0103af14)

    #define VPADRead ((int (*)(int controller, VPADData *buffer, unsigned int num, int *error))0x011293d0)

    /* Some really usefull addresses */
    #define FS_INSTALL_ADDR     0x011e0000 // where the fs functions are copied in memory
    #define KERN_BASE_VIRT      0xA0000000 // 0xC1000000
    #define KERN_BASE_PHYS      0x31000000 // 0x10000000
#endif

/* Server IP where to retrieve the rpx and for logs */
#define SERVER_IP           0xC0A80130

/* Some stuff for the display */
#define PRINT_TEXT1(x, y, str) { OSScreenPutFontEx(1, x, y, str); }
#define PRINT_TEXT2(x, y, _fmt, ...) { __os_snprintf(msg, 80, _fmt, __VA_ARGS__); OSScreenPutFontEx(1, x, y, msg); }
#define BTN_PRESSED (BUTTON_LEFT | BUTTON_RIGHT | BUTTON_UP | BUTTON_DOWN | BUTTON_A | BUTTON_B)

/* IP union */
typedef union u_serv_ip
{
    uint8_t  digit[4];
    uint32_t full;
} u_serv_ip;

/* Install functions */
static void InstallFS(int is_log_active, u_serv_ip ip);

/* main function */
void start() {
    /* Load a good stack */
    asm(
        "lis %r1, 0x1ab5 ;"
        "ori %r1, %r1, 0xd138 ;"
    );

    /* Check if kernel exploit is well installed */
    if (OSEffectiveToPhysical((void *)0xa0000000) != (void *)KERN_BASE_PHYS)
    {
        OSFatal("No kexploit");
    }
    else
    {
        /* ****************************************************************** */
        /*                                 Menu                               */
        /* ****************************************************************** */

        // Prepare screen
        int screen_buf0_size = 0;
        int screen_buf1_size = 0;
        uint32_t screen_color = 0; // (r << 24) | (g << 16) | (b << 8) | a;
        char msg[80];

         // Init screen and screen buffers
        OSScreenInit();
        screen_buf0_size = OSScreenGetBufferSizeEx(0);
        screen_buf1_size = OSScreenGetBufferSizeEx(1);
        OSScreenSetBufferEx(0, (void *)0xF4000000);
        OSScreenSetBufferEx(1, (void *)0xF4000000 + screen_buf0_size);

        // Clear screens
        OSScreenClearBufferEx(0, screen_color);
        OSScreenClearBufferEx(1, screen_color);

        // Flush the cache
        DCFlushRange((void *)0xF4000000, screen_buf0_size);
        DCFlushRange((void *)0xF4000000 + screen_buf0_size, screen_buf1_size);

        // Flip buffers
        OSScreenFlipBuffersEx(0);
        OSScreenFlipBuffersEx(1);
        
        // Prepare vpad
        unsigned int vpad_handle;

        // Set server ip with buttons
        int error;
        u_serv_ip ip;
        ip.full = SERVER_IP;
        uint8_t sel_ip = 3;
        uint8_t button_pressed = 1;
        uint8_t first_pass = 1;
        int use_fs_logs = 0;
        VPADData vpad_data;
        VPADRead(0, &vpad_data, 1, &error); //Read initial vpad status
        while (1)
        {
            // Refresh screen if needed
            if (button_pressed) { OSScreenFlipBuffersEx(1); OSScreenClearBufferEx(1, 0); }

            // Read vpad
            VPADRead(0, &vpad_data, 1, &error);

            // Title
            PRINT_TEXT1(23, 1, "-- SD Cafiine --");

            // Displays the current page
            PRINT_TEXT2(5, 5,  "1. IP : %3d.%3d.%3d.%3d (optional)", ip.digit[0], ip.digit[1], ip.digit[2], ip.digit[3]);
            PRINT_TEXT1(5, 6,  "2. Press A (or X to log FS functions, need server running)");

            PRINT_TEXT1(13 + 4 * sel_ip, 4, "vvv");

            // Check buttons
            if (!button_pressed)
            {
                // A Button
                if ((vpad_data.btn_hold & BUTTON_A) || (use_fs_logs = (vpad_data.btn_hold & BUTTON_X)))
                {
                    // Set wait message
                    OSScreenClearBufferEx(1, 0);
                    PRINT_TEXT1(27, 8, "Wait ...");
                    OSScreenFlipBuffersEx(1);
                    break;
                }

                // Left/Right Buttons
                if (vpad_data.btn_hold & BUTTON_LEFT ) sel_ip = !sel_ip ? sel_ip = 3 : --sel_ip;
                if (vpad_data.btn_hold & BUTTON_RIGHT) sel_ip = ++sel_ip % 4;

                // Up/Down Buttons
                if (vpad_data.btn_hold & BUTTON_UP  ) ip.digit[sel_ip] = ++ip.digit[sel_ip];
                if (vpad_data.btn_hold & BUTTON_DOWN) ip.digit[sel_ip] = --ip.digit[sel_ip];
            }

            // Print coffee and exit msg
            PRINT_TEXT1(0, 12, "    )))");
            PRINT_TEXT1(0, 13, "    (((");
            PRINT_TEXT1(0, 14, "  +-----+");
            PRINT_TEXT1(0, 15, "  | S D |]");
            PRINT_TEXT1(0, 16, "  `-----\'");
            PRINT_TEXT1(42, 17, "home button to exit ...");

            // Update screen
            if (first_pass) { OSScreenFlipBuffersEx(1); OSScreenClearBufferEx(1, 0); first_pass = 0; }

            // Home Button
            if (vpad_data.btn_hold & BUTTON_HOME)
                goto quit;

            // Button pressed ?
            button_pressed = (vpad_data.btn_hold & BTN_PRESSED) ? 1 : 0;
        }

        /* ****************************************************************** */
        /*                        Loadiine installation                       */
        /* ****************************************************************** */

        // Install fs functions
        InstallFS(use_fs_logs, ip);
    }

quit:
    _Exit();
}

/* Install FS functions */
#define FS_TYPE_ALL        0xff
#define FS_TYPE_REPLACE    0x01
#define FS_TYPE_LOG        0x02
static void InstallFS(int is_log_active, u_serv_ip ip)
{
    /* Copy in fs memory */
    unsigned int len = sizeof(fs_text_bin);
    unsigned char *loc = (unsigned char *)((char *)FS_INSTALL_ADDR + KERN_BASE_VIRT);

    while (len--) {
        loc[len] = fs_text_bin[len];
    }

    /* server IP address */
    ((unsigned int *)loc)[0] = is_log_active ? ip.full : 0;

    DCFlushRange(loc, sizeof(fs_text_bin));

    struct magic_t {
        void *real;
        void *replacement;
        void *call;
        uint  type;
    } *magic = (struct magic_t *)fs_magic_bin;
    len = sizeof(fs_magic_bin) / sizeof(struct magic_t);

    int *space = (int *)(loc + sizeof(fs_text_bin));
    /* Patch branches to it. */
    while (len--) {
        int real_addr = (int)magic[len].real;
        int repl_addr = (int)magic[len].replacement;
        int call_addr = (int)magic[len].call;
        uint type     = magic[len].type;

        if (((type & FS_TYPE_LOG) && is_log_active) || (type & FS_TYPE_REPLACE))
        {
            // set pointer to the real function
            *(int *)(KERN_BASE_VIRT + call_addr) = (int)space - KERN_BASE_VIRT;

            // fill the pointer of the real function
            *space = *(int *)(KERN_BASE_VIRT + real_addr);
            space++;

            // jump to real function skipping the "mflr r0" instruction
            *space = 0x48000002 | ((real_addr + 4) & 0x03fffffc);
            space++;
            DCFlushRange(space - 2, 8);

            // in the real function, replace the "mflr r0" instruction by a jump to the replacement function
            *(int *)(KERN_BASE_VIRT + real_addr) = 0x48000002 | (repl_addr & 0x03fffffc);
            DCFlushRange((int *)(KERN_BASE_VIRT + real_addr), 4);
        }
    }
}
