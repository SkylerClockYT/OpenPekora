package main

import (
	"AssetValidationV2/validate"
	"bytes"
	"log"
	"sync"

	"github.com/gofiber/fiber/v2"
)

type ValidationResponse struct {
	IsValid bool `json:"isValid"`
}

var asyncValidationMux sync.Mutex
var asyncValidationCount = 0

const asyncValidationLimit = 5

func tryBeforeValidation() bool {
	asyncValidationMux.Lock()
	defer asyncValidationMux.Unlock()
	if asyncValidationCount < asyncValidationLimit {
		asyncValidationCount++
		return true
	}
	return false
}

func afterValidation() {
	asyncValidationMux.Lock()
	asyncValidationCount--
	asyncValidationMux.Unlock()
}

func main() {
	app := fiber.New(fiber.Config{
		BodyLimit: 30 * 1024 * 1024, // 30 MB
	})

	app.Get("/", func(c *fiber.Ctx) error {
		return c.SendString("AssetValidationServiceV2 OK")
	})

	app.Post("/api/v1/validate-place", func(c *fiber.Ctx) error {
		if !tryBeforeValidation() {
			log.Println("Validation limit reached, rejecting request")
			c.Status(fiber.StatusTooManyRequests)
			c.Set("Retry-After", "5")
			return c.JSON(fiber.Map{
				"error": "Too many validations in progress, try again later.",
			})
		}
		defer afterValidation()
		body := c.Body()
		log.Println("validating place with size =", len(body))
		nReader := bytes.NewReader(body)
		isOk := validate.IsGameValid(nReader)
		return c.Status(200).JSON(ValidationResponse{
			IsValid: isOk,
		})
	})

	app.Post("/api/v1/validate-item", func(c *fiber.Ctx) error {
		if !tryBeforeValidation() {
			log.Println("Validation limit reached, rejecting request")
			c.Status(fiber.StatusTooManyRequests)
			c.Set("Retry-After", "5")
			return c.JSON(fiber.Map{
				"error": "Too many validations in progress, try again later.",
			})
		}
		defer afterValidation()
		body := c.Body()
		log.Println("validating item with size =", len(body))
		nReader := bytes.NewReader(body)
		isOk := validate.IsItemValid(nReader)
		return c.Status(200).JSON(ValidationResponse{
			IsValid: isOk,
		})
	})
	app.Post("/api/v1/validate-animation", func(c *fiber.Ctx) error {
		if !tryBeforeValidation() {
			log.Println("Validation limit reached, rejecting request")
			c.Status(fiber.StatusTooManyRequests)
			c.Set("Retry-After", "5")
			return c.JSON(fiber.Map{
				"error": "Too many validations in progress, try again later.",
			})
		}
		defer afterValidation()
		body := c.Body()
		log.Println("validating model with size =", len(body))
		nReader := bytes.NewReader(body)
		isOk := validate.IsAnimationValid(nReader)
		return c.Status(200).JSON(ValidationResponse{
			IsValid: isOk,
		})
	})

	app.Post("/api/v1/validate-model", func(c *fiber.Ctx) error {
		if !tryBeforeValidation() {
			log.Println("Validation limit reached, rejecting request")
			c.Status(fiber.StatusTooManyRequests)
			c.Set("Retry-After", "5")
			return c.JSON(fiber.Map{
				"error": "Too many validations in progress, try again later.",
			})
		}
		defer afterValidation()
		body := c.Body()
		log.Println("validating model with size =", len(body))
		nReader := bytes.NewReader(body)
		isOk := validate.IsModelValid(nReader)
		return c.Status(200).JSON(ValidationResponse{
			IsValid: isOk,
		})
	})

	log.Fatal(app.Listen(":4300"))
}
