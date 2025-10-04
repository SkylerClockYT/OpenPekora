package validate

import (
	"fmt"
	"io"
	"log"

	"github.com/robloxapi/rbxfile"
	"github.com/robloxapi/rbxfile/rbxl"
)

func LoadFile(reader io.Reader) (*rbxfile.Root, error) {
	root, warn, err := rbxl.Decoder{}.Decode(reader)
	if err != nil {
		return nil, err
	}
	if warn != nil {
		fmt.Println("[info] read warning:", warn)
	}
	return root, nil
}

func IsAnimationValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid animation file:", err)
		return false
	}

	childSet := make(map[*rbxfile.Instance]struct{})
	for _, inst := range file.Instances {
		for _, child := range inst.Children {
			childSet[child] = struct{}{}
		}
	}

	var rootKeyframes []*rbxfile.Instance

	for _, inst := range file.Instances {
		if _, isChild := childSet[inst]; isChild {
			continue
		}

		switch inst.ClassName {
		case "KeyframeSequence":
			rootKeyframes = append(rootKeyframes, inst)
		default:
			log.Printf("Invalid animation: unsupported top-level instance class %q\n", inst.ClassName)
			return false
		}
	}

	if len(rootKeyframes) == 0 {
		log.Println("Invalid animation: no KeyframeSequence found at root")
		return false
	}

	for _, kfs := range rootKeyframes {
		for _, child := range kfs.Children {
			if child.ClassName != "Keyframe" {
				log.Printf("Invalid animation: unsupported child class %q in KeyframeSequence\n", child.ClassName)
				return false
			}
		}
	}

	return true
}

func IsItemValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid item file:", err)
		return false
	}
	services := make(map[string]*rbxfile.Instance)
	for _, item := range file.Instances {
		if item.IsService {
			services[item.ClassName] = item
		}
	}
	//log.Println("item data", file, services)
	return len(services) == 0
}

func IsModelValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid model file:", err)
		return false
	}

	for _, inst := range file.Instances {
		if inst.IsService {
			log.Printf("Invalid model: contains service %q\n", inst.ClassName)
			return false
		}
	}

	childSet := make(map[*rbxfile.Instance]struct{})
	for _, inst := range file.Instances {
		for _, child := range inst.Children {
			childSet[child] = struct{}{}
		}
	}

	var rootModels []*rbxfile.Instance
	var rootScripts []*rbxfile.Instance

	for _, inst := range file.Instances {
		if _, isChild := childSet[inst]; isChild {
			continue
		}

		switch inst.ClassName {
		case "Model", "Tool", "Folder":
			rootModels = append(rootModels, inst)
		case "Script", "LocalScript", "ModuleScript":
			rootScripts = append(rootScripts, inst)
		default:
			log.Printf("Invalid model: unsupported top-level instance class %q\n", inst.ClassName)
			return false
		}
	}

	if len(rootScripts) == 1 && len(rootModels) == 0 {
		return true
	}

	if len(rootModels) == 1 && len(rootScripts) == 0 {
		root := rootModels[0]
		if len(root.Children) == 0 {
			log.Println("Invalid model: root Model has no children.")
			return false
		}
		// all valid classes
		validClasses := map[string]bool{
			// Base building parts
			"Part":            true,
			"MeshPart":        true,
			"SpecialMesh":     true,
			"WedgePart":       true,
			"CornerWedgePart": true,
			"UnionOperation":  true,
			"TrussPart":       true,
			"VehicleSeat":     true,
			"Seat":            true,
			"SpawnLocation":   true,

			// Meshes
			"BlockMesh":    true,
			"CylinderMesh": true,

			// Constraints & joints
			"Attachment":           true,
			"Motor6D":              true,
			"Weld":                 true,
			"WeldConstraint":       true,
			"HingeConstraint":      true,
			"BallSocketConstraint": true,
			"RodConstraint":        true,

			// Visual effects
			"Decal":           true,
			"Texture":         true,
			"SurfaceGui":      true,
			"BillboardGui":    true,
			"PointLight":      true,
			"SpotLight":       true,
			"SurfaceLight":    true,
			"ParticleEmitter": true,
			"Trail":           true,
			"Fire":            true,
			"Smoke":           true,

			// Skybox
			"Sky": true,

			// Sounds
			"Sound":                true,
			"SoundGroup":           true,
			"EqualizerSoundEffect": true,
			"ReverbSoundEffect":    true,

			// Containers
			"Folder":             true,
			"LuaSourceContainer": true,

			// UI elements
			"ScreenGui":      true,
			"TextLabel":      true,
			"TextButton":     true,
			"ImageLabel":     true,
			"ImageButton":    true,
			"Frame":          true,
			"ScrollingFrame": true,
			"UIListLayout":   true,
			"UIGridLayout":   true,
			"UICorner":       true,
			"UIStroke":       true,
			"UIPadding":      true,

			// Scripts
			"Script":       true,
			"LocalScript":  true,
			"ModuleScript": true,
		}

		for _, child := range root.Children {
			if !validClasses[child.ClassName] && child.ClassName != "Model" {
				log.Printf("Invalid model: unsupported child class %q in root model.\n", child.ClassName)
				return false
			}
		}

		return true
	}

	log.Printf("Invalid model: expected exactly 1 root Model or 1 root Script, found %d models and %d scripts.\n", len(rootModels), len(rootScripts))
	return false
}

func IsGameValid(reader io.Reader) bool {
	file, err := LoadFile(reader)
	if err != nil {
		log.Println("Invalid place file:", err)
		return false
	}
	services := make(map[string]*rbxfile.Instance)
	for _, item := range file.Instances {
		if item.IsService {
			services[item.ClassName] = item
		}
	}
	//fmt.Println("all services", services)
	if _, exists := services["Lighting"]; !exists {
		return false
	}
	_, workspaceExists := services["Workspace"]
	if !workspaceExists {
		return false
	}

	return true
}
