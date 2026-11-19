param(
    [string]$Source = (Join-Path $PSScriptRoot 'Background-Sheet-Horizontal.png'),
    [string]$Destination = (Join-Path $PSScriptRoot '../../Assets/PixelArt/Background-Sheet.png')
)

Add-Type -AssemblyName System.Drawing

$frameWidth = 232
$frameHeight = 131
$columns = 7
$rows = 6
$frameCount = $columns * $rows
$sourceBitmap = [System.Drawing.Bitmap]::new($Source)
$targetBitmap = $null
try {
    if ($sourceBitmap.Width -ne $frameWidth * $frameCount -or $sourceBitmap.Height -ne $frameHeight) {
        throw "Expected a 42-frame, 9744 x 131 horizontal sheet. Found $($sourceBitmap.Width) x $($sourceBitmap.Height)."
    }

    $targetBitmap = [System.Drawing.Bitmap]::new(
        $frameWidth * $columns, $frameHeight * $rows,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    $sourceRect = [System.Drawing.Rectangle]::new(0, 0, $sourceBitmap.Width, $sourceBitmap.Height)
    $targetRect = [System.Drawing.Rectangle]::new(0, 0, $targetBitmap.Width, $targetBitmap.Height)
    $sourceData = $sourceBitmap.LockBits($sourceRect,
        [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $targetData = $targetBitmap.LockBits($targetRect,
        [System.Drawing.Imaging.ImageLockMode]::WriteOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $pixelRow = New-Object byte[] ($frameWidth * 4)
        for ($frame = 0; $frame -lt $frameCount; $frame++) {
            $column = $frame % $columns
            $row = [int][Math]::Floor($frame / $columns)
            for ($y = 0; $y -lt $frameHeight; $y++) {
                $sourceOffset = $y * $sourceData.Stride + $frame * $frameWidth * 4
                $targetOffset = ($row * $frameHeight + $y) * $targetData.Stride + $column * $frameWidth * 4
                [System.Runtime.InteropServices.Marshal]::Copy(
                    [System.IntPtr]::Add($sourceData.Scan0, $sourceOffset), $pixelRow, 0, $pixelRow.Length)
                [System.Runtime.InteropServices.Marshal]::Copy(
                    $pixelRow, 0, [System.IntPtr]::Add($targetData.Scan0, $targetOffset), $pixelRow.Length)
            }
        }
    }
    finally {
        $targetBitmap.UnlockBits($targetData)
        $sourceBitmap.UnlockBits($sourceData)
    }

    $targetBitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "Repacked $frameCount frames into $($targetBitmap.Width) x $($targetBitmap.Height): $Destination"
}
finally {
    if ($null -ne $targetBitmap) { $targetBitmap.Dispose() }
    $sourceBitmap.Dispose()
}
