#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet_bin="${DOTNET_BIN:-dotnet}"
configuration="${CONFIGURATION:-Debug}"
target_framework="net8.0"
runtime_identifier="${RUNTIME_IDENTIFIER:-osx-arm64}"
install_dir="${HOME}/Library/Application Support/McNeel/Rhinoceros/8.0/MacPlugIns/RhinoImageStudio.rhp"
output_dir="${repo_root}/build/${configuration}/${target_framework}"
backend_output_dir="${repo_root}/build/${configuration}/mac-backend-${runtime_identifier}"

"${dotnet_bin}" build "${repo_root}/src/RhinoImageStudio.Mac.sln" --configuration "${configuration}"
"${dotnet_bin}" publish "${repo_root}/src/RhinoImageStudio.Backend/RhinoImageStudio.Backend.csproj" \
  --configuration "${configuration}" \
  --framework "${target_framework}" \
  --runtime "${runtime_identifier}" \
  --self-contained true \
  --output "${backend_output_dir}"

mkdir -p "${install_dir}"
mkdir -p "${install_dir}/Backend"
cp "${output_dir}/RhinoImageStudio.rhp" "${install_dir}/RhinoImageStudio.rhp"
cp "${output_dir}/RhinoImageStudio.deps.json" "${install_dir}/RhinoImageStudio.deps.json"
cp "${output_dir}/RhinoImageStudio.runtimeconfig.json" "${install_dir}/RhinoImageStudio.runtimeconfig.json"
cp "${output_dir}/RhinoImageStudio.Shared.dll" "${install_dir}/RhinoImageStudio.Shared.dll"
cp "${output_dir}/System.Drawing.Common.dll" "${install_dir}/System.Drawing.Common.dll"
cp "${output_dir}/Microsoft.Win32.SystemEvents.dll" "${install_dir}/Microsoft.Win32.SystemEvents.dll"
cp -R "${backend_output_dir}"/* "${install_dir}/Backend/"

if [[ -f "${output_dir}/RhinoImageStudio.pdb" ]]; then
  cp "${output_dir}/RhinoImageStudio.pdb" "${install_dir}/RhinoImageStudio.pdb"
fi

if [[ -f "${output_dir}/RhinoImageStudio.Shared.pdb" ]]; then
  cp "${output_dir}/RhinoImageStudio.Shared.pdb" "${install_dir}/RhinoImageStudio.Shared.pdb"
fi

echo "Installed Rhino Image Studio macOS plugin:"
echo "${install_dir}"
echo "Restart Rhino 8, then run ImageStudioMacStatus, ImageStudioStartBackend, or ImageStudioOpen."
