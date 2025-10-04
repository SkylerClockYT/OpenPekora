import sharp, {Sharp} from "sharp";
import {Console} from "./CS.js";

export class Imager {
    private content: Buffer;
    public height = 0;
    public width = 0;
    public aspectRatio = 0;
    public imageFormat: ImagerFormat = ImagerFormat.Undefined;

    private sharp: sharp.Sharp | null = null;
    private metadata: sharp.Metadata | null = null;

    private constructor(content: Buffer) {
        this.content = content;
    }

    public GetImage(): Sharp | null {
        return this.sharp;
    }

    public GetMetadata(): sharp.Metadata | null {
        return this.metadata;
    }

    private async InitializeAsync(): Promise<void> {
        try {
            this.sharp = sharp(this.content);
            this.metadata = await this.sharp.metadata();
        } catch (e) {
            Console.Error("Invalid image provided");
            return Promise.resolve();
        }

        if (!this.metadata || !this.metadata.format) {
            Console.Error("Invalid image provided");
            return  Promise.resolve();
        }

        this.height = this.metadata.height || 0;
        this.width = this.metadata.width || 0;
        this.aspectRatio = this.width !== 0 && this.height !== 0 ? this.width / this.height : 0;

        switch (this.metadata.format.toLowerCase()) {
            case "png":
                this.imageFormat = ImagerFormat.PNG;
                break;
            case "jpeg":
            case "jpg":
                this.imageFormat = ImagerFormat.JPEG;
                break;
            case "gif":
                this.imageFormat = ImagerFormat.GIF;
                break;
            case "bmp":
                this.imageFormat = ImagerFormat.BMP;
                break;
            default:
                Console.Error("Invalid image provided");
                return  Promise.resolve();
        }
    }

    public static async ReadAsync(content: Buffer): Promise<Imager> {
        const img = new Imager(content);
        await img.InitializeAsync();
        return img;
    }
}

export enum ImagerFormat {
    Undefined = 0,
    PNG,
    JPEG,
    GIF,
    BMP,
}
