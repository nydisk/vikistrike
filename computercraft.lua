local cYaw = peripheral.wrap("left")
local cPitch = peripheral.wrap("right")
local br = peripheral.find("block_reader")

local P_GAIN = 5.0
local TOLERANCE = 0.5

local function getCurrentYaw()
    return -br.getBlockData().CannonYaw
end

local function getCurrentPitch()
    return -br.getBlockData().CannonPitch
end

local function pControl(current, target, controller, invert)
    local err = target - current
    if invert then err = -err end
    if math.abs(err) < TOLERANCE then
        controller.setTargetSpeed(0)
        return true
    end
    controller.setTargetSpeed(math.max(-256, math.min(256, err * P_GAIN)))
    return false
end

local function aimAt(targetYaw, targetPitch)
    while true do
        local yawDone  = pControl(getCurrentYaw(),   targetYaw,   cYaw,   false)
        local pitchDone = pControl(getCurrentPitch(), targetPitch, cPitch, true)
        if yawDone and pitchDone then
            cYaw.setTargetSpeed(0)
            cPitch.setTargetSpeed(0)
            return
        end
        sleep(0.05)
    end
end

local ws = http.websocket("ws://localhost:858")

local x, y, z = gps.locate()
ws.send(textutils.serialiseJSON({ type = "register", x = x, y = y, z = z }))
print("registered at " .. x .. ", " .. y .. ", " .. z)

while true do
    local msg = ws.receive()
    if msg then
        local data = textutils.unserialiseJSON(msg)
        if data.type == "aim" then
            print("aiming to yaw=" .. data.yaw .. " pitch=" .. data.pitch)
            aimAt(data.yaw, data.pitch)
            ws.send(textutils.serialiseJSON({ type = "ready" }))
            print("ready")
        elseif data.type == "fire" then
            sleep(data.delay / 1000)
            rs.setOutput("back", true)
            sleep(0.5)
            rs.setOutput("back", false)
            print("fire!")
        end
    end
end