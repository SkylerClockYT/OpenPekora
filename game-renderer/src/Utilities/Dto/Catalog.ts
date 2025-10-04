import {IsInt, IsOptional, Max, IsDefined, IsBoolean, IsString} from "class-validator";

export class PlayerRenderRequest {
    @IsDefined()
    @IsInt()
    userId!: number;

    @IsOptional()
    @IsInt()
    @Max(60)
    jobExpiration = 20;
}

export class AnimationRenderRequest {
    @IsDefined()
    @IsString()
    characterAppearanceUrl!: string;

    @IsDefined()
    @IsString()
    animationUrl!: string;

    @IsOptional()
    @IsInt()
    @Max(60)
    jobExpiration?: number = 20;
}

export class AssetRenderRequest {
    @IsDefined()
    @IsInt()
    assetId!: number;

    @IsOptional()
    @IsInt()
    @Max(60)
    jobExpiration?: number = 20;

    @IsOptional()
    @IsBoolean()
    isFace?: boolean = false;
}

export class BodyPartRenderRequest {
    @IsDefined()
    @IsString()
    assetUrl!: string;

    @IsOptional()
    @IsInt()
    @Max(60)
    jobExpiration?: number = 20;

}
export class PackageRenderRequest {
    @IsDefined()
    @IsString()
    assetUrls!: number;

    @IsOptional()
    @IsInt()
    @Max(60)
    jobExpiration?: number = 20;
}

export class PlaceRenderRequest {
    @IsDefined()
    @IsInt()
    placeId!: number;

    @IsDefined()
    @IsInt()
    x!: number;

    @IsDefined()
    @IsInt()
    y!: number;

    @IsOptional()
    @IsInt()
    @Max(60)
    jobExpiration?: number = 20;
}

