@{
    RootModule        = 'PSTetris.dll'
    ModuleVersion     = '0.2.0'
    GUID              = 'a3e4b8f2-7c6d-4e5a-9f1b-2d3c4e5f6a7b'
    Author            = 'romero126'
    CompanyName       = 'romero126'
    Copyright         = '(c) romero126. All rights reserved.'
    Description       = 'A fully playable Tetris game that runs directly in your PowerShell console. Supports ANSI text and Sixel pixel graphics.'

    PowerShellVersion = '5.1'
    DotNetFrameworkVersion = '4.6.1'

    CmdletsToExport   = @('Start-Tetris')
    FunctionsToExport = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    PrivateData = @{
        PSData = @{
            Tags         = @('Tetris', 'Game', 'Console', 'ANSI', 'Sixel', 'Terminal')
            LicenseUri   = 'https://github.com/romero126/PSTetris/blob/master/LICENSE'
            ProjectUri   = 'https://github.com/romero126/PSTetris'
            ReleaseNotes = @'
## 0.2.0
- Add restart option on game over screen (R to restart, Q to quit)
- Autoscale game to fit terminal window size (both ANSI and Sixel renderers)
- Improve Sixel detection: DA1 probe first, env-var heuristics as fallback
- Add support for Windows Terminal and VSCode Sixel auto-detection
- Use alternate screen buffer to preserve terminal scrollback
'@
        }
    }
}
