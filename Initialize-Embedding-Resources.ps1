$files = @(
    @{ Name = 'model.onnx';            Url = 'https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx?download=true';          Size = '~480 KB'  }
    @{ Name = 'model.onnx_data';       Url = 'https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx_data?download=true';     Size = '~1.23 GB' }
    @{ Name = 'tokenizer.json';        Url = 'https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.json?download=true';            Size = '~20 MB'   }
    @{ Name = 'tokenizer.model';       Url = 'https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.model?download=true';           Size = '~4.7 MB'  }
    @{ Name = 'tokenizer_config.json'; Url = 'https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer_config.json?download=true';    Size = '~1.2 MB'  }
)

$destination = Join-Path $PSScriptRoot '.embedding_resources'
if (-not (Test-Path $destination)) {
    New-Item -ItemType Directory -Path $destination | Out-Null
}

Write-Host "Local embedding resources are under initialization. The resources will be saved to : $destination"

function Download-File($url, $outputPath, $name, $size) {
    $client = [System.Net.Http.HttpClient]::new()
    $dst = $null
    try {
        $response = $client.GetAsync($url, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        $total = $response.Content.Headers.ContentLength
        $src = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
        $dst = [System.IO.File]::OpenWrite($outputPath)
        $buf = [byte[]]::new(1MB)
        $read = 0L
        $bytes = 0
        while (($bytes = $src.Read($buf, 0, $buf.Length)) -gt 0) {
            $dst.Write($buf, 0, $bytes)
            $read += $bytes
            if ($total) {
                $pct    = [int]($read * 100 / $total)
                $readMB = [math]::Round($read / 1MB, 1)
                $totMB  = [math]::Round($total / 1MB, 1)
                Write-Progress -Activity "Downloading $name ($size)" -PercentComplete $pct -Status "$readMB MB / $totMB MB"
            }
        }
        Write-Progress -Activity "Downloading $name ($size)" -Completed
    } finally {
        if ($dst) { $dst.Dispose() }
        $client.Dispose()
    }
}

$i = 0
foreach ($file in $files) {
    $i++
    $outputPath = Join-Path $destination $file.Name

    if (Test-Path $outputPath) {
        continue
    }

    Write-Host "[$i/$($files.Count)] Downloading $($file.Name) ($($file.Size))..." -ForegroundColor Cyan

    try {
        Download-File $file.Url $outputPath $file.Name $file.Size
        Write-Host "  Finished" -ForegroundColor Green
    } catch {
        Write-Host "  Failed: $_" -ForegroundColor Red
        if (Test-Path $outputPath) { Remove-Item $outputPath -Force }
    }

    Write-Host ''
}

Write-Host 'Local embedding resources are ready.' -ForegroundColor Green
