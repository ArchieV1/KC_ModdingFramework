REM This will only work on Windows (Untested)
REM Another script is needed for Unix-Like

REM TODO does not find locally installed mods. Just steam mods
@echo off
setlocal enabledelayedexpansion
set "game_id=569480"
set "search_dir=KCModdingFramework"

echo Searching for folders with "%search_dir%" in "\steamapps\workshop\content\%game_id%\" across all drives...

for %%i in (A B C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    REM For each Drive letter
    if exist "%%i:\steamapps\workshop\content\%game_id%\" (
        REM If has steam workshop content for KC on the drive
        for /d %%d in ("%%i:\steamapps\workshop\content\%game_id%\*") do (
            REM For each Workshop Content for KC
            if exist "%%d\.Shared\ArchieV1\%search_dir%\" (
                REM If has mod contains folder structure: \.Shared\ArchieV1\KCModdingFramework (So has KC as a dependency)
                REM Print in format: 
                REM C:\steamapps\workshop\content\569408\1111111?True
                echo %%~nd^?True
            ) else (
                REM Print in format: 
                REM C:\steamapps\workshop\content\569408\1111111?False
                echo %%~nd^?False
            )
        )
    )
)

if not defined found (
    echo No folders with "%search_dir%" found in any drive.
)