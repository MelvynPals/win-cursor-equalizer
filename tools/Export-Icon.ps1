param()
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $repoPath 'references\icon.svg'
$assetPath = Join-Path $repoPath 'Assets'
$scratchPath = Join-Path $repoPath ('.local\icon-export-' + [guid]::NewGuid().ToString('N'))
$edgePath = @(
    'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Microsoft\Edge\Application\msedge.exe'
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $edgePath) { throw 'Microsoft Edge is required to render the source SVG.' }
New-Item -ItemType Directory -Path $scratchPath -Force | Out-Null
New-Item -ItemType Directory -Path $assetPath -Force | Out-Null
$svgBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($sourcePath))
$htmlPath = Join-Path $scratchPath 'icon.html'
$rasterPath = Join-Path $scratchPath 'source.png'
$html = '<!doctype html><html><head><style>html,body{margin:0;width:100%;height:100%;overflow:hidden}img{width:100%;height:100%;display:block}</style></head><body><img src="data:image/svg+xml;base64,' + $svgBase64 + '"></body></html>'
[IO.File]::WriteAllText($htmlPath, $html)
$arguments = @('--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
    '--hide-scrollbars', '--window-size=1024,1024',
    ('--user-data-dir="' + (Join-Path $scratchPath 'profile') + '"'),
    ('--screenshot="' + $rasterPath + '"'), ([Uri]$htmlPath).AbsoluteUri)
$browser = Start-Process -FilePath $edgePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $browser.WaitForExit(30000)) { throw 'SVG rendering timed out.' }
if (-not (Test-Path -LiteralPath $rasterPath)) { throw 'SVG rendering did not produce an image.' }
Copy-Item -LiteralPath $rasterPath -Destination (Join-Path $repoPath 'references\icon.png') -Force
Add-Type -AssemblyName System.Drawing
$source = [Drawing.Image]::FromFile($rasterPath)
try {
    $frames = @()
    $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
    foreach ($size in $sizes) {
        $bitmap = New-Object Drawing.Bitmap($size, $size)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $stream = New-Object IO.MemoryStream
        try {
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($source, (New-Object Drawing.Rectangle(0, 0, $size, $size)))
            $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
            $frames += ,$stream.ToArray()
            if ($size -eq 256) { $bitmap.Save((Join-Path $assetPath 'AppLogo.png'), [Drawing.Imaging.ImageFormat]::Png) }
        }
        finally { $graphics.Dispose(); $bitmap.Dispose(); $stream.Dispose() }
    }
    $file = [IO.File]::Create((Join-Path $assetPath 'AppIcon.ico'))
    $writer = New-Object IO.BinaryWriter($file)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
            $offset += $frames[$i].Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
    }
    finally { $writer.Dispose(); $file.Dispose() }
}
finally { $source.Dispose() }
Write-Output 'Exported references/icon.png, Assets/AppLogo.png and Assets/AppIcon.ico from references/icon.svg.'
