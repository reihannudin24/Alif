#!/usr/bin/env python3
"""
Unity .meta file generator for Alif assets.
Ensures every newly created sprite, texture, yarn script, or data file
has an official Unity 6000-compliant paired .meta file with unique GUID.
"""

import uuid
from pathlib import Path


def generate_guid() -> str:
    """Generate a clean 32-character lowercase hex Unity GUID."""
    return uuid.uuid4().hex


def create_texture_meta(
    meta_path: Path,
    pixels_per_unit: int = 100,
    filter_mode: int = 0, # 0 = Point (pixel art), 1 = Bilinear
    sprite_mode: int = 1, # 1 = Single Sprite
    pivot_x: float = 0.5,
    pivot_y: float = 0.0,
    guid: str = None
) -> str:
    """Creates a Unity TextureImporter .meta file for 2D sprites."""
    guid = guid or generate_guid()
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter_mode}
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: {sprite_mode}
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 7
  spritePivot: {{x: {pivot_x}, y: {pivot_y}}}
  spritePixelsToUnits: {pixels_per_unit}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
"""
    meta_path.write_text(content, encoding="utf-8")
    return guid


def create_text_script_meta(meta_path: Path, guid: str = None) -> str:
    """Creates a TextScriptImporter .meta file for Yarn dialogue or plain text."""
    guid = guid or generate_guid()
    content = f"""fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta_path.write_text(content, encoding="utf-8")
    return guid


def create_folder_meta(meta_path: Path, guid: str = None) -> str:
    """Creates a DefaultImporter .meta file for Unity folders."""
    guid = guid or generate_guid()
    content = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta_path.write_text(content, encoding="utf-8")
    return guid


def ensure_meta_for_file(file_path: Path, pixels_per_unit: int = 100) -> str:
    """Ensures a matching .meta exists for a given file or directory. If not, generates one."""
    if file_path.is_dir():
        meta_path = file_path.with_name(file_path.name + ".meta")
        if meta_path.exists():
            return "EXISTS"
        return create_folder_meta(meta_path)

    meta_path = file_path.with_suffix(file_path.suffix + ".meta")
    if meta_path.exists():
        return "EXISTS"
    
    ext = file_path.suffix.lower()
    if ext in [".png", ".jpg", ".jpeg", ".tga"]:
        return create_texture_meta(meta_path, pixels_per_unit=pixels_per_unit)
    elif ext in [".yarn", ".txt", ".json", ".csv"]:
        return create_text_script_meta(meta_path)
    else:
        return create_text_script_meta(meta_path)
