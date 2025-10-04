--	// FileName: MeCommandMessage.lua
--	// Written by: TheGamer101
--	// Description: Create a message label for a me command message.

local clientChatModules = script.Parent.Parent
local ChatSettings = require(clientChatModules:WaitForChild("ChatSettings"))
local ChatConstants = require(clientChatModules:WaitForChild("ChatConstants"))
local util = require(script.Parent:WaitForChild("Util"))

function CreateMeCommandMessageLabel(messageData, channelName)
	local message = messageData.Message
	local extraData = messageData.ExtraData or {}
	local useFont = extraData.Font or Enum.Font.SourceSansItalic
	local useTextSize = extraData.TextSize or ChatSettings.ChatWindowTextSize
	local useChatColor = Color3.new(1, 1, 1)
	local numNeededSpaces = 0

	local BaseFrame, BaseMessage = util:CreateBaseMessage("", useFont, useTextSize, useChatColor)
	local ChannelButton = nil

	if channelName ~= messageData.OriginalChannel then
		local formatChannelName = string.format("{%s}", messageData.OriginalChannel)
		ChannelButton = util:AddChannelButtonToBaseMessage(BaseMessage, formatChannelName, useNameColor)
		numNeededSpaces = util:GetNumberOfSpaces(formatChannelName, useFont, useTextSize) + 1
	end

	local function UpdateTextFunction(messageObject)
		if messageData.IsFiltered then
			BaseMessage.Text = string.rep(" ", numNeededSpaces) .. messageObject.FromSpeaker .. " " .. string.sub(messageObject.Message, 5)
		else
			local messageLength = string.len(messageObject.FromSpeaker) + messageObject.MessageLength - 4
			BaseMessage.Text = string.rep(" ", numNeededSpaces) .. string.rep("_", messageLength)
		end
	end

	UpdateTextFunction(messageData)

	local function GetHeightFunction()
		return util:GetMessageHeight(BaseMessage, BaseFrame)
	end

	local AnimParams = {}
	AnimParams.Text_TargetTransparency = 0
	AnimParams.Text_CurrentTransparency = 0
	AnimParams.Text_NormalizedExptValue = 1
	AnimParams.TextStroke_TargetTransparency = 0.75
	AnimParams.TextStroke_CurrentTransparency = 0.75
	AnimParams.Text_NormalizedExptValue = 1

	local function FadeInFunction(duration, CurveUtil)
		AnimParams.Text_TargetTransparency = 0
		AnimParams.TextStroke_TargetTransparency = 0.75
		AnimParams.Text_NormalizedExptValue = CurveUtil:NormalizedDefaultExptValueInSeconds(duration)
		AnimParams.TextStroke_NormalizedExptValue = CurveUtil:NormalizedDefaultExptValueInSeconds(duration)
	end

	local function FadeOutFunction(duration, CurveUtil)
		AnimParams.Text_TargetTransparency = 1
		AnimParams.TextStroke_TargetTransparency = 1
		AnimParams.Text_NormalizedExptValue = CurveUtil:NormalizedDefaultExptValueInSeconds(duration)
		AnimParams.TextStroke_NormalizedExptValue = CurveUtil:NormalizedDefaultExptValueInSeconds(duration)
	end

	local function AnimGuiObjects()
		BaseMessage.TextTransparency = AnimParams.Text_CurrentTransparency
		BaseMessage.TextStrokeTransparency = AnimParams.TextStroke_CurrentTransparency

		if ChannelButton then
			ChannelButton.TextTransparency = AnimParams.Text_CurrentTransparency
			ChannelButton.TextStrokeTransparency = AnimParams.TextStroke_CurrentTransparency
		end
	end

	local function UpdateAnimFunction(dtScale, CurveUtil)
		AnimParams.Text_CurrentTransparency = CurveUtil:Expt(
				AnimParams.Text_CurrentTransparency,
				AnimParams.Text_TargetTransparency,
				AnimParams.Text_NormalizedExptValue,
				dtScale
		)
		AnimParams.TextStroke_CurrentTransparency = CurveUtil:Expt(
				AnimParams.TextStroke_CurrentTransparency,
				AnimParams.TextStroke_TargetTransparency,
				AnimParams.TextStroke_NormalizedExptValue,
				dtScale
		)

		AnimGuiObjects()
	end

	return {
		[util.KEY_BASE_FRAME] = BaseFrame,
		[util.KEY_UPDATE_TEXT_FUNC] = UpdateTextFunction,
		[util.KEY_GET_HEIGHT] = GetHeightFunction,
		[util.KEY_FADE_IN] = FadeInFunction,
		[util.KEY_FADE_OUT] = FadeOutFunction,
		[util.KEY_UPDATE_ANIMATION] = UpdateAnimFunction
	}
end

return {
	[util.KEY_MESSAGE_TYPE] = ChatConstants.MessageTypeMeCommand,
	[util.KEY_CREATOR_FUNCTION] = CreateMeCommandMessageLabel
}
