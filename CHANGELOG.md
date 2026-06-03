# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

<!-- Headers should be listed in this order: Added, Changed, Deprecated, Removed, Fixed, Security -->
## [3.4.0] - 2026-02-13

### Changed
- Updated Unity Editor to 6.3 LTS (`6000.3.0f1`).
- Updated `com.unity.ai.navigation` to version `2.0.10`.
- Updated `com.unity.ide.rider` to version `3.0.39`.
- Updated `com.unity.multiplayer.center` to version `1.0.1`.
- Updated `com.unity.inputsystem` to version `1.16.0`.
- Updated `com.unity.render-pipelines.universal` to version `17.3.0`.
- Updated `com.unity.test-framework` to version `1.6.0`.
- Updated `com.unity.timeline` to version `1.8.10`.
- Updated `com.unity.ugui` to version `2.0.0`.
- Updated `com.unity.xr.interaction.toolkit` to version `3.4.0`.
- Updated `com.unity.xr.management` to version `4.5.4`.
- Updated `com.unity.xr.openxr` to version `1.16.1`.

### Removed
- Removed `com.unity.textmeshpro` due to upgrade to Unity UI (`com.unity.ugui`) 2.0.0.

### Fixed
- Fixed Preset Manager project settings to use all the presets installed with the Starter Assets sample.
- Fixed or suppressed warnings in project scripts after Unity 6.3 version upgrade.
- Fixed warning with `Assets/XRI_Examples/Environment/Shaders/Waterfall_Ripples.shadergraph` after Unity 6.3 version upgrade by updating Color node.
- Fixed the door with lock system so that the key can be turned once in the lock, plus added the ability to start with the door locked.
- Fixed some of the `Rigidbody` settings for the `DoorLocked` prefab to make it easier to manipulate.

## [3.1.2] - 2025-05-16

### Changed
- Updated `com.unity.inputsystem` to version `1.13.1`
- Updated `com.unity.xr.interaction.toolkit` to version `3.1.2`
- Updated `com.unity.xr.management` to version `4.5.1`
- Updated `com.unity.xr.openxr` to version `1.14.3`
- Updated `com.unity.ai.navigation` to version `1.1.6`
- Updated `com.unity.timeline` to version `1.7.7`
- Changed to using `com.unity.feature.development` version `1.0.2` instead of IDE-specific packages.
- Updated Unity Editor to `2022.3.56f1`

### Removed
- Removed `com.unity.xr.oculus`.

## [3.0.7] - 2024-11-27

### Added
- Added snap volume to Teleportation Anchors to improve reticle behavior and make it easier to aim at.

### Changed
- Updated XR Interaction Toolkit to 3.0.7
- Updated Input System to 1.11.2
- Updated TextMeshPro to 3.0.9
- Updated XR Plugin Management to 4.5.0
- Updated Unity Editor to 2022.3.53f1
- Changed Graphics API on Android from OpenGLES3 to Vulkan.

### Fixed
- Fixed Grab Move GameObject to be activated as a scene override to allow grab move to be controlled by the Locomotion Settings table.
- Fixed duplicate Locomotion Manager in scenes.
- Fixed main scene by removing extra spatial keyboard.

## [3.0.3] - 2024-06-12

### Changed
- Updated XR Interaction Toolkit to 3.0.3
- Updated Input System to 1.8.1

## [2.5.3] - 2024-03-25

### Changed
 - Updated XR Interaction Toolkit to 2.5.3

## [2.5.2] - 2024-03-06

### Added
 - New station to show how to utilize interaction focus state
 - New station with different types of climbables

### Changed
 - Updated XR Interaction Toolkit to 2.5.2
 - Updated Unity Editor to 2021.3.33f1

## [2.4.0] - 2023-06-22

### Changed
 - Updated XR Interaction Toolkit to 2.4.0
 - Updated Unity Editor to 2021.3.27f1

## [2.3.2] - 2023-05-09
### Added
 - Added Gaze station

### Changed
 - Update XR Interaction Toolkit to 2.3.2.
 - Updated all interactables to use the Affordance System instead of the Hover Highlight scripts.
 - Updated documentation with new features and Gaze station information.

### Removed
- Removed the scripts and prefabs associated with hover highlight.

## [2.3.0] - 2023-02-06
### Changed
 - Update XR Interaction Toolkit to 2.3.0.

## [2.2.0] - 2022-12-16
### Changed
 - Update XR Interaction Toolkit to 2.2.0.

## [2.1.0] - 2022-09-30
### Added
 - First version of the XRI Examples

### This is the first release of the updated XRI Examples project.
